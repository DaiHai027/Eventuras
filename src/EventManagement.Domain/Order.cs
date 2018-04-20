using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace losol.EventManagement.Domain
{
	public class Order
	{
		public enum OrderStatus
		{
			Draft,
			Verified,
			Invoiced,
			Cancelled
		}


		[Required]
		public int OrderId { get; set; }
		public string UserId { get; set; }
		public int RegistrationId { get; set; }

		/**
			Allowed transitions:
			Draft
			Draft -> Cancelled
			Draft -> Verified -> Cancelled
			Draft -> Verified -> Invoiced -> Cancelled
		 */
		private OrderStatus _status = OrderStatus.Draft;
		public OrderStatus Status { 
			get => _status; 
			set {
				switch(value) {
					case OrderStatus.Draft:
						throw new InvalidOperationException("Orders cannot be set as draft.");
					case OrderStatus.Verified:
						if(_status != OrderStatus.Draft)
						{
							throw new InvalidOperationException("Only draft orders can be verified.");
						}
						break;
					case OrderStatus.Invoiced:
						if(_status != OrderStatus.Verified)
						{
							throw new InvalidOperationException("Only verified orders can be invoiced.");
						}
						break;

					case OrderStatus.Cancelled:
						// Anything can be cancelled
						break;

				}
				_status = value;
			} 
		}

		// From registration, should be Participant details, if Customer details
		// does not exist.
		public string CustomerName { get; set; }
		public string CustomerEmail { get; set; }
		public string CustomerVatNumber { get; set; }
		public string CustomerInvoiceReference { get; set; }
		public int? PaymentMethodId { get; set; }

		public DateTime OrderTime { get; set; } = DateTime.UtcNow;

		// Comments are from the user registered
		public string Comments { get; set; }

		// Log is information from the system. Ie registration time and user.
		public string Log { get; set; }

		// Navigational properties
		public Registration Registration { get; set; }
		public PaymentMethod PaymentMethod { get; set; }
		public ApplicationUser User { get; set; }
		public List<OrderLine> OrderLines { get; set; }


		public void AddLog(string text = null)
		{
			var logText = $"{OrderTime.ToString("u")}: {Status}";
			if(!string.IsNullOrWhiteSpace(text))
			{
				text += $": {text}";
			}
			Log += logText + "\n";
		}

		public bool CanEdit => 
			Status == OrderStatus.Draft || Status == OrderStatus.Verified;


		public void MarkAsVerified()
		{
			Status = OrderStatus.Verified;
			this.AddLog();
		}

		public void MarkAsCancelled()
		{
			Status = OrderStatus.Cancelled;
			this.AddLog();
		}

		public void MarkAsInvoiced()
		{
			Status = OrderStatus.Invoiced;
			this.AddLog();
		}

	}
}
