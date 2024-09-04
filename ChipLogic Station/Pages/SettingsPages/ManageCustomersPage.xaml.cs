using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;  // Include this namespace for KeyEventArgs
using System.Windows.Media;
using ChipLogic.Configuration;
using ChipLogic.Database;
using ChipLogic.Utils;

namespace ChipLogic.Pages.Settings
{
    public partial class ManageCustomersPage : Page
    {
        private DatabaseConfig config;
        private List<string> allCustomers;  // Store all customers for searching

        public ManageCustomersPage()
        {
            InitializeComponent();
            config = ConfigManager.LoadOrCreateConfig();  // Initialize the config variable
            Logger.Initialize(config.InstallPath); // Initialize Logger
            LoadCustomers();  // Load existing customers
        }

        // Load customers from the database and populate the ComboBox and other UI elements
        private void LoadCustomers()
        {
            try
            {
                allCustomers = DBCommands.GetAllCustomers(config.ConnectionString);
                CustomerComboBox.ItemsSource = allCustomers;  // Set ComboBox's items source
                DeleteCustomerComboBox.ItemsSource = allCustomers;  // Set ComboBox's items source for deletion
            }
            catch (Exception ex)
            {
                Logger.Log($"Error loading customers: {ex.Message}", isError: true, debug: config.Debug);
                MessageBox.Show($"Error loading customers: {ex.Message}");
            }
        }

        // Event handler for filtering customers in the ComboBox based on text input
        private void CustomerComboBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                string searchText = comboBox.Text.ToLower();

                // Filter customers that contain the search text
                var filteredCustomers = allCustomers.Where(c => c.ToLower().Contains(searchText)).ToList();

                comboBox.ItemsSource = filteredCustomers;
                comboBox.IsDropDownOpen = true;  // Keep the dropdown open while typing
            }
        }

        // Event handler for selecting a customer from the ComboBox
        private void CustomerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CustomerComboBox.SelectedItem != null)
            {
                string selectedCustomer = CustomerComboBox.SelectedItem.ToString();
                var customerDetails = DBCommands.GetCustomerDetails(selectedCustomer, config.ConnectionString);

                // Populate fields with the selected customer's details
                EditCustomerNumberTextBox.Text = customerDetails.CustomerNumber;
                EditCompanyNameTextBox.Text = customerDetails.CompanyName;
                EditContactNameTextBox.Text = customerDetails.ContactName;
                EditContactPhoneTextBox.Text = customerDetails.ContactPhone;
                EditContactEmailTextBox.Text = customerDetails.ContactEmail;
                EditCustomerAddressTextBox.Text = customerDetails.CustomerAddress;
                EditIsActiveCheckBox.IsChecked = customerDetails.IsActive;
            }
        }

        // Event handler for adding a new customer
        private void AddCustomerButton_Click(object sender, RoutedEventArgs e)
        {
            string customerNumber = CustomerNumberTextBox.Text.Trim();
            string companyName = CompanyNameTextBox.Text.Trim();
            string contactName = ContactNameTextBox.Text.Trim();
            string contactPhone = ContactPhoneTextBox.Text.Trim();
            string contactEmail = ContactEmailTextBox.Text.Trim();
            string customerAddress = CustomerAddressTextBox.Text.Trim();
            bool isActive = IsActiveCheckBox.IsChecked ?? false;

            // Check if the necessary fields are not empty
            if (string.IsNullOrWhiteSpace(customerNumber) || string.IsNullOrWhiteSpace(companyName))
            {
                MessageBox.Show("Customer number and company name cannot be empty.");
                return;
            }

            try
            {
                // Check if the customer number already exists
                bool customerExists = DBCommands.CheckCustomerNumberExists(customerNumber, config.ConnectionString);
                if (customerExists)
                {
                    MessageBox.Show("The customer number already exists. Please enter a unique customer number.");
                    return;
                }

                // Add customer to the database
                DBCommands.AddCustomer(customerNumber, companyName, contactName, contactPhone, contactEmail, customerAddress, isActive, config.ConnectionString);
                Logger.Log($"Customer {companyName} added successfully.", isError: false, debug: config.Debug);
                MessageBox.Show($"Customer {companyName} added successfully.");
                LoadCustomers();  // Reload customers to reflect the new addition
            }
            catch (Exception ex)
            {
                Logger.Log($"Error adding customer: {ex.Message}", isError: true, debug: config.Debug);
                MessageBox.Show($"Error adding customer: {ex.Message}");
            }
        }


        // Event handler for saving changes to an existing customer
        private void SaveCustomerButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedCustomer = CustomerComboBox.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedCustomer))
            {
                MessageBox.Show("Please select a customer to edit.");
                return;
            }

            string companyName = EditCompanyNameTextBox.Text;
            string contactName = EditContactNameTextBox.Text;
            string contactPhone = EditContactPhoneTextBox.Text;
            string contactEmail = EditContactEmailTextBox.Text;
            string customerAddress = EditCustomerAddressTextBox.Text;
            bool isActive = EditIsActiveCheckBox.IsChecked ?? false;

            try
            {
                // Update customer details in the database
                DBCommands.UpdateCustomer(selectedCustomer, companyName, contactName, contactPhone, contactEmail, customerAddress, isActive, config.ConnectionString);
                Logger.Log($"Customer {companyName} updated successfully.", isError: false, debug: config.Debug);
                MessageBox.Show($"Customer {companyName} updated successfully.");
                LoadCustomers();  // Reload customers to reflect the updated details
            }
            catch (Exception ex)
            {
                Logger.Log($"Error updating customer: {ex.Message}", isError: true, debug: config.Debug);
                MessageBox.Show($"Error updating customer: {ex.Message}");
            }
        }

        // Event handler for deleting a customer
        private void DeleteCustomerButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedCustomer = DeleteCustomerComboBox.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedCustomer))
            {
                MessageBox.Show("Please select a customer to delete.");
                return;
            }

            try
            {
                // Delete customer from the database
                DBCommands.DeleteCustomer(selectedCustomer, config.ConnectionString);
                Logger.Log($"Customer {selectedCustomer} deleted successfully.", isError: false, debug: config.Debug);
                MessageBox.Show($"Customer {selectedCustomer} deleted successfully.");
                LoadCustomers();  // Reload customers to reflect the deletion
            }
            catch (Exception ex)
            {
                Logger.Log($"Error deleting customer: {ex.Message}", isError: true, debug: config.Debug);
                MessageBox.Show($"Error deleting customer: {ex.Message}");
            }
        }

        // Placeholder text removal for TextBox
        private void RemovePlaceholderText(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (textBox.Text == "Customer Number" || textBox.Text == "Company Name" || textBox.Text == "Contact Name" ||
                    textBox.Text == "Contact Phone" || textBox.Text == "Contact Email" || textBox.Text == "Customer Address")
                {
                    textBox.Text = "";
                    textBox.Foreground = new SolidColorBrush(Colors.Black);
                }
            }
        }

        // Placeholder text addition for TextBox
        private void AddPlaceholderText(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && string.IsNullOrWhiteSpace(textBox.Text))
            {
                if (textBox.Name == "CustomerNumberTextBox")
                {
                    textBox.Text = "Customer Number";
                }
                else if (textBox.Name == "CompanyNameTextBox")
                {
                    textBox.Text = "Company Name";
                }
                else if (textBox.Name == "ContactNameTextBox")
                {
                    textBox.Text = "Contact Name";
                }
                else if (textBox.Name == "ContactPhoneTextBox")
                {
                    textBox.Text = "Contact Phone";
                }
                else if (textBox.Name == "ContactEmailTextBox")
                {
                    textBox.Text = "Contact Email";
                }
                else if (textBox.Name == "CustomerAddressTextBox")
                {
                    textBox.Text = "Customer Address";
                }

                textBox.Foreground = new SolidColorBrush(Colors.Gray);
            }
        }
    }
}
