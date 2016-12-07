﻿using System;
using System.Linq;

namespace Travel_Agency
{
    [Serializable]
    class Order : IComparable<Order>
    {
        public Offer TravelOffer { get; set; }
        public Client OrderClient { get; set; }
        public Worker ServiceWorker { get; set; }
        public DateTime OrderRegisterDate { get; set; }
        public DateTime TravelStartDate { get; set; }
        private static int _howManyOrders;
        public int OrderNumber { get; private set; }
        public int OrderPrice { get; set; }
        public int OrderClientsAmount { get; set; }

        public static event MainForm.EmailSendEventHandler<Order> EmailSend;

        public Order(Offer offer, Client client, Worker worker, int orderClientsAmount, DateTime travelStartDate, ILogger loggerBox, ILogger loggerFile, ILogger loggerMail)
        {
            TravelOffer = offer;
            ServiceWorker = worker;
            OrderClient = client;
            OrderRegisterDate = DateTime.Now;
            TravelStartDate = travelStartDate;
            _howManyOrders++;
            OrderNumber = Program.allOrders.OrderByDescending(x => x.Key).FirstOrDefault().Key + 1;
            OrderClientsAmount = orderClientsAmount;
            OrderPrice = offer.Price * orderClientsAmount;
            AddOrderPriceToBudget(OrderPrice);
            if (loggerBox != null)
                loggerBox.WriteToLog(this, OrderRegisterDate, "Created order", OrderClient.Email);
            if (loggerFile != null)
                loggerFile.WriteToLog(this, OrderRegisterDate, "Created order", OrderClient.Email);
            if (loggerMail != null)
            {
                if (EmailSend != null)
                    EmailSend(this, new EmailSendEventArgs(OrderClient.Email, "Created order", OrderRegisterDate, loggerMail));
            }
        }

        public Order()
        {
        }

        public void AddOrderPriceToBudget(int orderPrice)
        {
            Budget.AddToBudget(orderPrice);
        }

        public void ReduceOrderPriceFromBudget(int orderPrice)
        {
            Budget.ReduceFromBudget(orderPrice);
        }

        public int CompareTo(Order other)
        {
            if (TravelStartDate > other.TravelStartDate) return 1;
            else if (TravelStartDate < other.TravelStartDate) return -1;
            else return 0;
        }

        public bool IsActive()
        {
            if (TravelStartDate > DateTime.Today) return true;
            else return false;
        }

        public override string ToString()
        {
            return "Order number: " + OrderNumber.ToString() + Environment.NewLine + "" + TravelOffer.ToString() + Environment.NewLine + "Client: " + OrderClient.Name + " " + OrderClient.LastName + Environment.NewLine + "Worker: " + ServiceWorker.Name + " " + ServiceWorker.LastName + Environment.NewLine + "Order price: €" + OrderPrice.ToString() + Environment.NewLine + "Travelers amount: " + OrderClientsAmount.ToString() + Environment.NewLine + "Travel start date: " + TravelStartDate.ToShortDateString() + Environment.NewLine + "Order registered on: " + OrderRegisterDate.ToShortDateString();
        }
    }
}