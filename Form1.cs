using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Data.SQLite;



namespace WebBrowserPage
{
    public partial class Form1 : Form
    {
        //HttpClient instance for sending HTTP requests
        private readonly HttpClient httpClient;
        private readonly string default_home_page = "https://www.hw.ac.uk/";

        //Stacks to manage back and forward navigation
        private Stack<string> backHistory = new Stack<string>();
        private Stack<string> forwardHistory = new Stack<string>();

        ////File paths for saving and loading history and favourites
        private static readonly string historyfile = "history.txt";
        private static readonly string favouritesFile = "favourites.txt";
        private string BulkdownloadFile = "bulk.txt";
        public Form1()
        {
            InitializeComponent();
            
            InitializeDatabase(); // Initialize the SQLite database

            httpClient = new HttpClient();

            //Load default homepage, favourites, and history on startup
            Load_HomePage();
            Load_Favourites();
            Load_History();
            this.KeyPreview = true;

        }

        //Loads the default home page URL into the browser
        private async void Load_HomePage()
        {
            try
            {
                textBox.Text = default_home_page;
                await LoadUrl(default_home_page);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred:{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
        //Display the status code and HTML content
        private void DisplayContent(string htmlcontent, string statusCode)
        {
            status_textbox.Text = $"Status Code: {statusCode}";
            html_Textbox.Text = htmlcontent;
        }
        //Loads a specified URL,updating navigation history if needed
        private async Task LoadUrl(string url, bool isNavigatingBackForward = false)
        {
            try
            {
                var response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    //Fetch HTML content and status code

                    string htmlContent = await response.Content.ReadAsStringAsync();
                    string statusText = $"{(int)response.StatusCode}  {response.StatusCode}";
                    DisplayContent(htmlContent, statusText);

                    ////Update navigation history if not navigating back/forward

                    if (!isNavigatingBackForward)
                    {
                        backHistory.Push(url);
                        forwardHistory.Clear();
                    }
                    //Save homepage html to a local file 
                    string homePagePath = Path.Combine(Environment.CurrentDirectory, "homepage.txt");
                    File.WriteAllText(homePagePath, htmlContent);
                    Add_History(url);


                }
                else
                {
                    HttpError_handler(response.StatusCode);
                }

            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Error loading page:{ex.Message}", "HTTP Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An Unexpected error occurred:{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // ensure the URL starts with "http" or "https"
        private string verify_url(string url)
        {

            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                url = "https://" + url;
            }
            return url;
        }

        //Attempts to load the URL entered in textbox
        private async void go_button_Click(object sender, EventArgs e)
        {
            try
            {
                string url = textBox.Text;
                if (string.IsNullOrWhiteSpace(url))
                {
                    MessageBox.Show("Enter valid URL.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                url = verify_url(url);
                await LoadUrl(url);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred:{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }


        //Loads homepage when home button is clicked
        private void Home_button_Click(object sender, EventArgs e)
        {
            Load_HomePage();
        }

        //Navigate to previous page in history
        private async void Backward_button_Click(object sender, EventArgs e)
        {
            try
            {
                if (backHistory.Count > 1)
                {
                    string currentURL = backHistory.Pop();          //Remove current URL
                    forwardHistory.Push(currentURL);                //Add it to forward history

                    string previousURL = backHistory.Pop();         //Get previous URL
                    textBox.Text = previousURL;                     //Update URL textbox
                    await LoadUrl(previousURL, true);               //Load previous URL
                }
                else
                {
                    MessageBox.Show("No previous URL to navigate.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while navigating back!{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        //Navigate forward in history 
        private async void forward_button_Click(object sender, EventArgs e)
        {
            try
            {
                if (forwardHistory.Count > 0)
                {
                    string nextURL = forwardHistory.Pop();          //Get next URL
                    backHistory.Push(nextURL);                      //Add it back to history

                    textBox.Text = nextURL;                         //Update URL textbox
                    await LoadUrl(nextURL, true);                   //Load next URL
                }
                else
                {
                    MessageBox.Show("No next URL to navigate", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while navigating forward!{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }


        //Refresh current page
        private async void refresh_button_Click_1(object sender, EventArgs e)
        {

            try
            {
                string currentURL = textBox.Text;
                if (!string.IsNullOrWhiteSpace(currentURL))
                {
                    await LoadUrl(currentURL, true);
                    MessageBox.Show("The page is refreshed successfully !", "Page Refresh", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("No valid URL to refresh ! ", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while refreshing the page :{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Adds the current URL to favourites with user-specified name
        private void add_to_fav_button_Click_1(object sender, EventArgs e)
        {
            string url = textBox.Text;

            if (!string.IsNullOrWhiteSpace(url))
            {
                url = verify_url(url);      //Verify URL before adding to favourites
                string name = Microsoft.VisualBasic.Interaction.InputBox("Enter the name for URL:", "Add to favourites", "Name");

                if (!string.IsNullOrWhiteSpace(name))
                {
                    string favItem = $"{name}-{url}";
                    favourite_listbox.Items.Add(favItem);  // Display in listbox immediately

                    try
                    {
                        // Insert into database
                        string dbPath = Path.Combine(Environment.CurrentDirectory, "favorites.db");
                        using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                        {
                            conn.Open();
                            string query = "INSERT INTO Favourites (URL, AddedDate, AddedTime) VALUES (@url, @date, @time)";
                            using (var cmd = new SQLiteCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@url", favItem);
                                cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd"));
                                cmd.Parameters.AddWithValue("@time", DateTime.Now.ToString("HH:mm:ss"));
                                cmd.ExecuteNonQuery();
                            }
                        }

                        MessageBox.Show("URL added to favourites successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"An error occurred while saving to favourites: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("You must provide a name for the favourite!", "Invalid name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show("Enter valid URL to add to favourites.", "Invalid URL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Loads saved favourites from database into the favourites listbox
        private void Load_Favourites()
        {
            favourite_listbox.Items.Clear();
            try
            {
                string dbPath = Path.Combine(Environment.CurrentDirectory, "favorites.db");
                using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    conn.Open();
                    string query = "SELECT URL FROM Favourites";
                    using (var cmd = new SQLiteCommand(query, conn))
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            favourite_listbox.Items.Add(reader["URL"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading favourites: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

     
        // Edits the selected favourite entry in the list
        private void Edit_fav_button_Click(object sender, EventArgs e)
        {
            if (favourite_listbox.SelectedItem is string selectedFav)
            {
                string[] parts = selectedFav.Split('-');
                string currentName = parts[0];
                string currentUrl = parts[1];

                string newName = Microsoft.VisualBasic.Interaction.InputBox("Edit Name:", "Edit favourite Name", currentName);
                if (string.IsNullOrWhiteSpace(newName))
                {
                    MessageBox.Show("The name cannot be empty!", "Invalid Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string newUrl = Microsoft.VisualBasic.Interaction.InputBox("Edit URL:", "Edit Favourite URL", currentUrl);
                if (string.IsNullOrWhiteSpace(newUrl) || (!newUrl.StartsWith("http://") && !newUrl.StartsWith("https://")))
                {
                    MessageBox.Show("Please enter a valid URL. Must start with http:// or https://", "Invalid URL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    string updatedFav = $"{newName}-{newUrl}";

                    // Update in database
                    string dbPath = Path.Combine(Environment.CurrentDirectory, "favorites.db");
                    using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                    {
                        conn.Open();
                        string query = "UPDATE Favourites SET URL = @newUrl WHERE URL = @oldUrl";
                        using (var cmd = new SQLiteCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@newUrl", updatedFav);
                            cmd.Parameters.AddWithValue("@oldUrl", selectedFav);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // Update listbox
                    favourite_listbox.Items.Remove(selectedFav);
                    favourite_listbox.Items.Add(updatedFav);

                    MessageBox.Show("Favourite updated successfully!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while updating the favourite: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a URL from the favourites list to edit.", "No Selection", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            }
        }

        // Delete favourite entry
        private void Delete_fav_button_Click(object sender, EventArgs e)
        {
            if (favourite_listbox.SelectedItem is string selectedFav)
            {
                try
                {
                    string dbPath = Path.Combine(Environment.CurrentDirectory, "favorites.db");
                    using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                    {
                        conn.Open();
                        string query = "DELETE FROM Favourites WHERE URL = @url";
                        using (var cmd = new SQLiteCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@url", selectedFav);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    favourite_listbox.Items.Remove(selectedFav);
                    MessageBox.Show("Favourite deleted successfully!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting favourite: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a favourite from the favourites list to delete", "No Selection", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            }
        }
         private async void favourite_listbox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (favourite_listbox.SelectedItem is string selectedFav)
            {
                // Deselect any selected item in history list
                history_listBox.ClearSelected();

                string[] parts = selectedFav.Split('-');
                if (parts.Length == 2)
                {
                    string url = verify_url(parts[1].Trim());       // verify the URL formal and assign to the variable
                    textBox.Text = url;
                    await LoadUrl(url);
                }
            }

        }

        // save to the  favourites file
        private void Save_Favourites()
        {
            List<string> fav_list = favourite_listbox.Items.Cast<string>().ToList();
            System.IO.File.WriteAllLines(favouritesFile, fav_list);
        }

        private void Add_History(string url)
        {
            string validate_url = verify_url(url);
            if (!history_listBox.Items.Contains(validate_url))
            {

                history_listBox.Items.Add(validate_url);        //Add URL to history listbox

                System.IO.File.AppendAllLines(historyfile, new[] { validate_url });


            }
        }

        //Loads saved history from file into the  listbox
        private void Load_History()
        {
            history_listBox.Items.Clear();
            if (System.IO.File.Exists(historyfile))
            {
                var history = System.IO.File.ReadAllLines(historyfile); ;
                foreach (var entry in history)
                {
                    history_listBox.Items.Add(entry);
                }
            }

        }


        private async void history_listBox_MouseClick(object sender, MouseEventArgs e)
        {
            if (history_listBox.SelectedItem is string selectedHistory)
            {
                // Deselect any selected item in favourites list
                favourite_listbox.ClearSelected();


                textBox.Text = selectedHistory;
                await LoadUrl(selectedHistory);

            }
        }

        // delete history entry
        private void Delete_hist_button_Click(object sender, EventArgs e)
        {
            if (history_listBox.SelectedItem is string selectedHistory)
            {
                RemoveHistory(selectedHistory);
                Load_History();
                MessageBox.Show("History deleted successfully !", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Please select a URL from the history list to delete.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        public void RemoveHistory(string url)
        {
            if (System.IO.File.Exists(historyfile))
            {
                var history = System.IO.File.ReadAllLines(historyfile).Where(entry => entry != url).ToList();
                System.IO.File.WriteAllLines(historyfile, history);
                history_listBox.Items.Remove(url);
            }
        }


        // Method to handle HTTP errors based on the status code received from the server
        private void HttpError_handler(HttpStatusCode statusCode)
        {
            string errorMessage = "An unknown error occurred !";
            string statusText;
            switch (statusCode)
            {
                case HttpStatusCode.OK:
                    statusText = $"200  OK";
                    break;

                case HttpStatusCode.BadRequest:

                    statusText = $"400 Bad request";
                    errorMessage = "400 Bad request: The server could not understand the request due to invalid syntax.";
                    break;

                case HttpStatusCode.Forbidden:
                    statusText = $"403 Forbidden";
                    errorMessage = "403 Forbidden: You donot have permission to access this resource.";
                    break;

                case HttpStatusCode.NotFound:
                    statusText = $"404 Not Found";
                    errorMessage = "404 Not Found: The resource you are looking for could not be found";
                    break;
                default:
                    statusText = $"Error:{(int)statusCode}  {statusCode}";
                    errorMessage = $"Error:{(int)statusCode} {statusCode}";
                    break;

            }
            status_textbox.Text = statusText;
            DisplayContent(errorMessage, statusText);
        }
        private async void bulk_download_Click(object sender, EventArgs e)
        {
            string filePath = Select_File();
            if (filePath != null)
            {
                await StartBulkDownload(filePath);
            }
        }
        private string Select_File()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Text files (*.txt)|*.txt";
                openFileDialog.Title = "Select Bulk Download File";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    return openFileDialog.FileName;
                }
            }
            return null;
        }

        private async Task StartBulkDownload(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
            {
                MessageBox.Show("The specified file doesnot exist.");
                return;
            }
            html_Textbox.Clear();
            html_Textbox.AppendText("Starting bulk download...\r\n\n");
            string[] urls = System.IO.File.ReadAllLines(filePath);
            List<string> results = new List<string>();

            foreach (var url in urls)
            {
                string trimmedUrl = url.Trim();
                if (!string.IsNullOrEmpty(trimmedUrl))
                {
                    var resultLine = await Fetch_Content(trimmedUrl);
                    results.Add(resultLine);
                }

            }
            html_Textbox.AppendText(string.Join(Environment.NewLine, results));
            html_Textbox.AppendText("\r\n Bulk download completed!");
        }

        private async Task<string> Fetch_Content(string url)
        {
            try
            {
                var response = await httpClient.GetAsync(url);
                var contentLength = (await response.Content.ReadAsByteArrayAsync()).Length;
                return $"Status Code:{response.StatusCode}|{contentLength} bytes |URL:{url}";

            }
            catch (HttpRequestException ex)
            {
                return $"Error fetching {url}:{ex.Message}";
            }
            catch (Exception ex)
            {
                return $"An unexpected error occurred while fetching{url}: {ex.Message}";
            }
        }

        // shortcut keys 
        private void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                go_button.PerformClick();
                e.SuppressKeyPress = true;

            }
            else if (e.Control && e.KeyCode == Keys.B && backHistory.Count > 0)
            {
                Backward_button.PerformClick();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.F && forwardHistory.Count > 0)
            {
                forward_button.PerformClick();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.R)
            {
                refresh_button.PerformClick();
                e.SuppressKeyPress = true;

            }
        }

        // display title of the wepage
        private void html_Textbox_TextChanged(object sender, EventArgs e)
        {
            string htmlContent = html_Textbox.Text;
            var titleStart = htmlContent.IndexOf("<title", StringComparison.OrdinalIgnoreCase);
            if (titleStart != -1)
            {
                titleStart = htmlContent.IndexOf(">", titleStart, StringComparison.OrdinalIgnoreCase);
            }
            var titleEnd = htmlContent.IndexOf("</title>", StringComparison.OrdinalIgnoreCase);

            if (titleStart != -1 && titleEnd != -1 && titleEnd > titleStart)
            {
                string pagetitle = htmlContent.Substring(titleStart + 1, titleEnd - (titleStart + 1)).Trim();
                title_textBox.Text = pagetitle;
            }
            if (titleStart != -1 && titleEnd != -1 && titleEnd > titleStart)
            {
                string pagetitle = htmlContent.Substring(titleStart + 1, titleEnd - (titleStart + 1)).Trim();


                if (pagetitle.Contains("|"))
                    pagetitle = pagetitle.Split('|').Last().Trim();

                title_textBox.Text = pagetitle;
            }

            else
            {
                title_textBox.Text = "No Title Found!";
            }
        }

        /*
         * Daatabase Initialization Method
         * 
         */

        private void InitializeDatabase()
        {
            // Path to the SQLite database file
            string dbPath = Path.Combine(Environment.CurrentDirectory, "favorites.db");

            // Create the database file if it doesn't exist
            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
            }

            // Open a connection to the database
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();

                // SQL query to create Favourites table if it doesn't exist
                string createTableQuery = @"
            CREATE TABLE IF NOT EXISTS Favourites (
            Id INTEGER PRIMARY KEY AUTOINCREMENT, 
            URL TEXT NOT NULL,               
            AddedDate TEXT NOT NULL,              
            AddedTime TEXT NOT NULL               
        );";

                // Execute the SQL command
                using (var cmd = new SQLiteCommand(createTableQuery, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

    }
}