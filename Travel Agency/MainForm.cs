﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Travel_Agency
{
    public partial class MainForm : Form
    {
        public delegate void EmailSendEventHandler<T>(T sender, EmailSendEventArgs e);
        public MainForm()
        {
            InitializeComponent();
            LoginForm loginForm = new LoginForm();
            loginForm.ShowDialog();
            Budget.Bankrupt += BankruptHandler;
            Font = new Font(FileInput.ReadSetting("Font name", "User.config"), Convert.ToInt32(FileInput.ReadSetting("Font size", "User.config")));
            FileInput fileInput = new FileInput();
            fileInput.ShowDialog();
            StartThreadQuantityUpdate();
        }

        public async void StartThreadQuantityUpdate()
        {
            await SetLabelsAwait();
        }

        private async Task SetLabelsAwait()
        {
            clientsQuantity.Text = (await GetLabelTextAsync(Program.allClients.Count, "Number of clients: "));
            offersQuantity.Text = (await GetLabelTextAsync(Program.allOffers.Count, "Number of offers: "));
            workersQuantity.Text = (await GetLabelTextAsync(Program.allWorkers.Count, "Number of workers: "));
            ordersQuantity.Text = (await GetLabelTextAsync(Program.allOrders.Count, "Number of orders: "));
            activeOrders.Text = (await GetLabelTextAsync(CheckActiveOrders(), "Active orders: "));
            if (!Budget.IsBankrupt())
            {
                budgetBalance.BackColor = DefaultBackColor;
                budgetBalance.Text = (await GetLabelTextAsync((int)Budget.Balance, "Budget balance: €"));
            }
        }

        Action<Label> MakeLabelBankrupt = (label) =>
        {
            label.Text = "BANKRUPT!!!";
            label.BackColor = Color.Red;
        };

        private void BankruptHandler(BankruptEventArgs args)
        {
            MakeLabelBankrupt(budgetBalance);
            MessageBox.Show("Travel agency reached bankrupt! Current balance: €" + args.CurrentBalance, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        private Task<string> GetLabelTextAsync(int number, string tag)
        {
            return Task.Run(() => GetLabelText(number, tag));
        }

        private string GetLabelText(int number, string tag)
        {
            return tag + number.ToString();
        }

        private int CheckActiveOrders()
        {
            int activeOrders = 0;
            if (Program.allOrders.Count > 0)
            {
                foreach (Order order in Program.allOrders.Values)
                {
                    if (order.TravelStartDate > DateTime.Today) activeOrders++;
                }
            }
            return activeOrders;
        }

        private Task SaveObjectsTask<T>(List<T> objects, string filePath, string tag)
        {
            Task task = new Task(() => SaveObjects(objects, filePath, tag));
            return task;
        }

        private void SaveObjects<T>(List<T> objects, string filePath, string tag)
        {
            Stream streamWriter = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            IFormatter formatter = new BinaryFormatter();
            byte[] tagByte = Encoding.ASCII.GetBytes(tag);
            streamWriter.Write(tagByte, 0, tag.Length);
            foreach (T item in objects)
            {
                formatter.Serialize(streamWriter, item);
            }
            streamWriter.Close();
        }

        private void GUI_FormClosed(object sender, FormClosedEventArgs e)
        {
            Thread saveObjectsThread = new Thread(Serialize);
            saveObjectsThread.Start();
            new Thread(SaveBudgetValues).Start();
            while (saveObjectsThread.ThreadState == System.Threading.ThreadState.Running){}
            MessageBox.Show("Data successfully updated!", "Success",  MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void SaveBudgetValues()
        {
            using (StreamWriter file = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\" + FileInput.ReadSetting("Budget file source", "App.config"), false))
            {
                file.WriteLine(Budget.Balance);
                file.WriteLine(Budget.Income);
                file.WriteLine(Budget.Outcome);
                file.WriteLine(Budget.Profit);
            }
        }

        private void Serialize()
        {
            foreach (Worker worker in Program.allWorkers.Values)
            {
                worker.ClearListOfOrders();
            }
            SaveObjectsTask(Program.allClients.Values.ToList(), FileInput.ClientsFilePath, "clients").RunSynchronously();
            SaveObjectsTask(Program.allOffers.Values.ToList(), FileInput.OffersFilePath, "offers").RunSynchronously();
            SaveObjectsTask(Program.allOrders.Values.ToList(), FileInput.OrdersFilePath, "orders").RunSynchronously();
            SaveObjectsTask(Program.allWorkers.Values.ToList(), FileInput.WorkersFilePath, "workers").RunSynchronously();
        }

        private void AddWorker_Click(object sender, EventArgs e)
        {
            AddWorkerForm addWorkerForm = new AddWorkerForm(this);
            addWorkerForm.ShowDialog();
        }

        private void AddOrder_Click(object sender, EventArgs e)
        {
            AddOrderForm addOrderForm = new AddOrderForm(this);
            addOrderForm.ShowDialog();
        }

        private void AddClient_Click(object sender, EventArgs e)
        {
            AddClientForm addClientForm = new AddClientForm(this);
            addClientForm.ShowDialog();
        }

        private void AddOffer_Click(object sender, EventArgs e)
        {
            AddOfferForm addOfferForm = new AddOfferForm(this);
            addOfferForm.ShowDialog();
        }

        private void ShowOffersButton_Click(object sender, EventArgs e)
        {
            if (Program.allOffers.Count > 0)
            {
                List<string> list = Program.allOffers.Values.Select(i => i.OfferNumber + ". " + i.TravelDestination + ", " + i.HotelRanking + ", " + i.Feeding + ", €" + i.Price).ToList();
                ShowObject showObject = new ShowObject(new BindingSource(list, null), typeof(Offer), this);
                showObject.Text = "Show offers";
                showObject.ShowDialog();
            }
            else MessageBox.Show("No offers!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void ShowOrdersButton_Click(object sender, EventArgs e)
        {
            if (Program.allOrders.Count > 0)
            {
                List<string> list = Program.allOrders.Values.Select(i => i.OrderNumber + ". " + i.OrderClient.Name + " " + i.OrderClient.LastName + " " + i.TravelOffer.TravelDestination).ToList();
                ShowObject showObject = new ShowObject(new BindingSource(list, null), typeof(Order), this);
                showObject.Text = "Show orders";
                showObject.ShowDialog();
            }
            else MessageBox.Show("No orders!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void ShowWorkersButton_Click(object sender, EventArgs e)
        {
            if (Program.allWorkers.Count > 0)
            {
                List<string> list = Program.allWorkers.Values.Select(i => i.WorkerNumber + ". " + i.Name + " " + i.LastName + ", " + i.Position).ToList();
                ShowObject showObject = new ShowObject(new BindingSource(list, null), typeof(Worker), this);
                showObject.Text = "Show workers";
                showObject.ShowDialog();
            }
            else MessageBox.Show("No workers!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void ShowClientsButton_Click(object sender, EventArgs e)
        {
            if (Program.allClients.Count > 0)
            {
                List<string> list = Program.allClients.Values.Select(i => i.Name + " " + i.LastName + " [Client number: " + i.ClientNumber + "]").ToList();
                ShowObject showObject = new ShowObject(new BindingSource(list, null), typeof(Client), this);
                showObject.Text = "Show clients";
                showObject.ShowDialog();
            }
            else MessageBox.Show("No clients!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void NearestDeparturesButton_Click(object sender, EventArgs e)
        {
            NearestDeparturesForm nearestDeparturesForm = new NearestDeparturesForm();
            nearestDeparturesForm.ShowDialog();
        }

        private void GUI_Load(object sender, EventArgs e)
        {
            BackgroundImage = new Bitmap(Properties.Resources.image);
        }

        private void SendEmailButton_Click(object sender, EventArgs e)
        {
            SendInformationByEmailForm sendByEmail = new SendInformationByEmailForm();
            sendByEmail.ShowDialog();
        }

        public static void PayOutSalaryHandler(ShowObject sender, EventArgs e)
        {
            Worker worker = Program.allWorkers[Convert.ToInt32(sender.objectBox.SelectedItem.ToString().Split('.').First())];
            if (Budget.Balance - worker.Salary > Convert.ToDouble(FileInput.ReadSetting("Limit of bankrupt", "App.config")))
            {
                MessageBox.Show("Paid out €" + worker.Salary + " to " + worker.Name + " " + worker.LastName, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                worker.PaySalary();
                sender.Dispose();
            }
            else
            {
                worker.PaySalary();
                sender.Dispose();
            }
            
        }

        private async void PayOutSalaryButton_Click(object sender, EventArgs e)
        {
            lock(Program.allWorkers)
            {
                if (Program.allWorkers.Count > 0)
                {
                    List<string> list = Program.allWorkers.Values.Select(i => i.WorkerNumber + ". " + i.Name + " " + i.LastName + ", " + i.Position).ToList();
                    ShowObject showObject = new ShowObject(new BindingSource(list, null), typeof(Worker), this);
                    showObject.Text = "Pay out salary";
                    showObject.showButton.Text = "Pay out salary";
                    showObject.showButton.Size = new Size(240, 23);
                    showObject.deleteButton.Visible = false;
                    showObject.ShowDialog();
                }
                else MessageBox.Show("No workers!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            await SetLabelsAwait();
        }

        private void ChangeWorkerShiftButton_Click(object sender, EventArgs e)
        {
            lock (Program.allWorkers)
            {
                if (Program.allWorkers.Count > 0)
                {
                    List<string> list = Program.allWorkers.Values.Select(i => i.WorkerNumber + ". " + i.Name + " " + i.LastName + ", " + i.Position).ToList();
                    ChangeShiftForm changeShiftForm = new ChangeShiftForm(new BindingSource(list, null));
                    changeShiftForm.establishmentComboBox.Items.AddRange(new object[] {
                    "Full time",
                    "3/4 time",
                    "1/2 time",
                    "1/4 time"});
                    changeShiftForm.ShowDialog();
                }
                else MessageBox.Show("No workers!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ChangeWorkerPositionButton_Click(object sender, EventArgs e)
        {
            lock (Program.allWorkers)
            {
                if (Program.allWorkers.Count > 0)
                {
                    List<string> list = Program.allWorkers.Values.Select(i => i.WorkerNumber + ". " + i.Name + " " + i.LastName + ", " + i.Position).ToList();
                    ChangeShiftForm changeShiftForm = new ChangeShiftForm(new BindingSource(list, null));
                    AddWorkerForm addWorker = new AddWorkerForm();
                    changeShiftForm.establishmentComboBox.Items.AddRange(new object[] {
                    "Operations manager",
                    "Quality control, safety, environmental manager",
                    "Accountant, bookkeeper, controller",
                    "Office manager",
                    "Receptionist",
                    "Foreperson, supervisor, lead person",
                    "Marketing manager",
                    "Purchasing manager",
                    "Shipping and receiving person or manager",
                    "Professional staff"});
                    changeShiftForm.establishmentLabel.Text = "Position: ";
                    changeShiftForm.ShowDialog();
                }
                else MessageBox.Show("No workers!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RaiseCutSalaryButton_Click(object sender, EventArgs e)
        {
            if (Program.allWorkers.Count > 0)
            {
                List<string> list = Program.allWorkers.Values.Select(i => i.WorkerNumber + ". " + i.Name + " " + i.LastName + ", " + i.Position).ToList();
                RaiseCutSalaryForm raiseCutSalaryForm = new RaiseCutSalaryForm(new BindingSource(list, null));
                raiseCutSalaryForm.ShowDialog();
            }
            else MessageBox.Show("No workers!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void ShowWorkerOrdersbutton_Click(object sender, EventArgs e)
        {
            if (Program.allWorkers.Count > 0)
            {
                List<string> list = Program.allWorkers.Values.Select(i => i.WorkerNumber + ". " + i.Name + " " + i.LastName + ", " + i.Position).ToList();
                ShowObject showObject = new ShowObject(new BindingSource(list, null), typeof(Offer), this);
                showObject.Text = "Show worker's orders";
                showObject.showButton.Text = "Show worker's orders";
                showObject.showButton.Size = new Size(240, 23);
                showObject.deleteButton.Visible = false;
                showObject.ShowDialog();
            }
            else MessageBox.Show("No workers!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}