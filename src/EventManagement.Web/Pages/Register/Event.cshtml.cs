﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using losol.EventManagement.Domain;
using losol.EventManagement.Infrastructure;
using losol.EventManagement.Pages.Account;
using losol.EventManagement.Services;
using losol.EventManagement.ViewModels;

namespace losol.EventManagement.Pages.Register
{
	public class EventRegistrationModel : PageModel
	{
		private readonly ApplicationDbContext _context;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly ILogger<LoginModel> _logger;
		private readonly IEmailSender _emailSender;
		private readonly AppSettings _appSettings;
		private readonly IHostingEnvironment _env;
		private readonly IRenderService _renderService;


		public EventRegistrationModel(
			ApplicationDbContext context,
			UserManager<ApplicationUser> userManager,
			ILogger<LoginModel> logger,
			IEmailSender emailSender,
			IOptions<AppSettings> appSettings,
			IHostingEnvironment env,
			IRenderService renderService
			)
		{
			_context = context;
			_userManager = userManager;
			_logger = logger;
			_emailSender = emailSender;
			_appSettings = appSettings.Value;
			_env = env;
			_renderService = renderService;
		}

		[BindProperty]
		public RegisterVM Registration { get; set; }
		public List<Product> Products { get; set; }

		public async Task PopulateProducts(EventInfo eventinfo)
		{
			this.Products = await _context.Products.Where(m => m.EventInfoId == eventinfo.EventInfoId)
				.Include(p => p.ProductVariants)
				.ToListAsync();
			this.Registration.Products = new ProductVM[Products.Count];
			for (int i = 0; i < Registration.Products.Length; i++)
			{
				var currentProduct = Products[i];
				Registration.Products[i] = new ProductVM
				{
					Value = currentProduct.ProductId,
					IsMandatory = currentProduct.MandatoryCount > 0,
					IsSelected = currentProduct.MandatoryCount > 0,
					SelectedVariantId = currentProduct
						.ProductVariants
						.Select(pv => pv.ProductVariantId as int?)
						.FirstOrDefault()
				};
			}
		}

		public async Task<IActionResult> OnGetAsync(int? id)
		{

			if (id == null)
			{
				return RedirectToPage("./Index");
			}

			Registration = new RegisterVM();
			var eventinfo = await _context.EventInfos.FirstOrDefaultAsync(m => m.EventInfoId == id);
			await PopulateProducts(eventinfo);

			if (eventinfo == null)
			{
				return NotFound();
			}
			else
			{
				Registration.EventInfo = eventinfo;
				Registration.EventInfoId = eventinfo.EventInfoId;
				Registration.EventInfoTitle = eventinfo.Title;
				Registration.EventInfoDescription = eventinfo.Description;
				Registration.PaymentMethodId = 2;  // TODO: Dirty
				Registration.PaymentMethods = _context.PaymentMethods.Where(m => m.Active == true).ToList();
			}
			return Page();
		}


		public async Task<IActionResult> OnPostAsync(int? id)
		{
			var eventInfo = await _context.EventInfos
										  .Include(e => e.Products)
										  .FirstOrDefaultAsync(m => m.EventInfoId == id);

			Registration.EventInfo = eventInfo;
			Registration.EventInfoId = eventInfo.EventInfoId;
			Registration.EventInfoTitle = eventInfo.Title;
			Registration.EventInfoDescription = eventInfo.Description;

			if (!ModelState.IsValid)
			{
				await PopulateProducts(eventInfo);
				return Page();
			}

			if (eventInfo.Products != null)
			{
				await PopulateProducts(eventInfo);
				var selectedProductIds = Registration.Products
													 .Where(rp => rp.IsSelected)
													 .Select(p => p.Value)
													 .ToList();

				var registeredProducts = eventInfo.Products
												  .Where(x => selectedProductIds.Contains(x.ProductId))
												  .ToList();

				// Get the list of product names along with id and variants ...
				var productNames = registeredProducts.Select(product =>
				{
					var variantId = Registration.Products
											  .Where(p => product.ProductId == p.Value)
											  .Select(p => p.SelectedVariantId)
											  .FirstOrDefault();

					var variantString = "";
					if (variantId != null)
					{
						var variantName = Products.Where(p => p.ProductId == product.ProductId)
											  .SelectMany(p => p.ProductVariants)
											  .Where(v => v.ProductVariantId == variantId)
											  .Select(v => v.Name)
											  .Single();

						variantString = $" ({variantId}. {variantName})";
					}

					return $"{product.ProductId}. {product.Name}{variantString}";
				});

				// ... and concatenate them together into the notes field
				Registration.Notes = String.Join(", ", productNames);
			}

			// Check if user exists with email registered
			var user = await _userManager.FindByEmailAsync(Registration.Email);

			if (user != null)
			{
				Registration.UserId = user.Id;
				_logger.LogInformation("Found existing user.");
			}
			else
			{
				// Create new user
				var newUser = new ApplicationUser { UserName = Registration.Email, Name = Registration.ParticipantName, Email = Registration.Email, PhoneNumber = Registration.Phone };
				var result = await _userManager.CreateAsync(newUser);

				if (result.Succeeded)
				{
					_logger.LogInformation("User created a new account with password.");

					Registration.UserId = newUser.Id;

				}
				foreach (var error in result.Errors)
				{
					ModelState.AddModelError(string.Empty, error.Description);
				}
			};

			// Any registrations for this user on this event?
			var registered = await _context.Registrations
				.Where(a =>
					a.UserId == Registration.UserId &&
					a.EventInfoId == Registration.EventInfoId)
				.FirstOrDefaultAsync();

			// If registration found
			if (registered != null)
			{
				_logger.LogWarning("Found existing registration:" + registered.RegistrationId);
				// Prepare an email to send out
				var emailVM = new EmailMessage()
				{
					Name = Registration.ParticipantName,
					Email = Registration.Email,
					Subject = "Du var allerede påmeldt!",
					Message = @"Vi hadde allerede registrert deg i systemet.
								Ta kontakt med ole@nordland-legeforening hvis du tror det er skjedd noe feil her!
								"
				};

				// If registered but not verified, just send reminder of verification. 
				if (registered.Verified == false)
				{
					var verificationUrl = Url.Action("Confirm", "Register", new { id = registered.RegistrationId, auth = registered.VerificationCode }, protocol: Request.Scheme);
					emailVM.Subject = "En liten bekreftelse bare...";
					emailVM.Message = $@"Vi hadde allerede registrert deg i systemet, men du har ikke bekreftet enda
								Ta kontakt med ole@nordland-legeforening hvis du tror det er skjedd noe feil her!
								<p><a href='{verificationUrl}'>Bekreft her</a></p>
								<p></p>
								<p>Hvis lenken ikke virker, så kan du kopiere inn teksten under i nettleseren:
								{verificationUrl} </p>";
				}

				var emailString = await _renderService.RenderViewToStringAsync("Templates/Email/StandardEmail", emailVM);
				await _emailSender.SendEmailAsync(emailVM.Email, emailVM.Subject, emailString);

				return RedirectToPage("/Info/EmailSent");
			}

			// If we came here, we should enter our new participant into the database!
			_logger.LogWarning("Starting new registration:");
			var newRegistration = new Registration();
			newRegistration.VerificationCode = GenerateRandomPassword(6);
			var entry = _context.Add(newRegistration);
			entry.CurrentValues.SetValues(Registration);

			// Save changes
			try
			{
				await _context.SaveChangesAsync();
			}
			catch (DbUpdateException ex)
			{
				_logger.LogError(ex.Message + ex.InnerException);
			}

			var confirmEmail = new ConfirmEventRegistration()
			{
				Name = Registration.ParticipantName,
				Phone = Registration.Phone,
				Email = Registration.Email,
				PaymentMethod = Registration.PaymentMethodId.ToString(),
				EventTitle = Registration.EventInfoTitle,
				EventDescription = Registration.EventInfoDescription
			};

			confirmEmail.VerificationUrl = Url.Action("Confirm", "Register", new { id = newRegistration.RegistrationId, auth = newRegistration.VerificationCode }, protocol: Request.Scheme);

			var email = await _renderService.RenderViewToStringAsync("Templates/Email/ConfirmEventRegistration", confirmEmail);
			await _emailSender.SendEmailAsync(Registration.Email, "Bekreft påmelding", email);

			return RedirectToPage("/Info/EmailSent");
		}

		public static string GenerateRandomPassword(int length = 6)
		{
			string[] randomChars = new[] {
				"ABCDEFGHJKLMNPQRSTUVWXYZ",    // uppercase 
                "abcdefghijkmnpqrstuvwxyz",    // lowercase
                "123456789"                   // digits
            };
			Random rand = new Random(Environment.TickCount);

			List<char> chars = new List<char>();

			for (int i = chars.Count; i < length; i++)
			{
				string rcs = randomChars[rand.Next(0, randomChars.Length)];
				chars.Insert(rand.Next(0, chars.Count),
					rcs[rand.Next(0, rcs.Length)]);
			}

			return new string(chars.ToArray());
		}

		#region View Models
		public class ProductVM
		{
			public bool IsSelected { get; set; } = false;
			public int Value { get; set; }
			public bool IsMandatory { get; set; } = false;
			public int? SelectedVariantId { get; set; } = null;
		}

		public class RegisterVM
		{
			public int EventInfoId { get; set; }
			public string EventInfoTitle { get; set; }
			public string EventInfoDescription { get; set; }
			public string UserId { get; set; }

			[Required]
			[StringLength(100)]
			[Display(Name = "Navn")]
			public string ParticipantName { get; set; }

			[Required]
			[EmailAddress]
			[Display(Name = "E-post")]
			public string Email { get; set; }

			[Required]
			[Display(Name = "Mobiltelefon")]
			public string Phone { get; set; }

			[Display(Name = "Arbeidsplass")]
			public string ParticipantEmployer { get; set; }

			[Display(Name = "Stilling")]
			public string ParticipantJobTitle { get; set; }

			[Display(Name = "Sted/by")]
			public string ParticipantCity { get; set; }

			[Display(Name = "Kommentar til påmelding. ")]
			public string Notes { get; set; }

			[Display(Name = "Organisasjonsnummer (må fylles ut for EHF-faktura)")]
			public string CustomerVatNumber { get; set; }

			[Display(Name = "Betaling")]
			public IEnumerable<PaymentMethod> PaymentMethods { get; set; }

			// Who pays for it?
			[Display(Name = "Fakturamottakers firmanavn")]
			public string CustomerName { get; set; }

			[Display(Name = "Fakturamottakers epost")]
			public string CustomerEmail { get; set; }

			[Display(Name = "Fakturareferanse")]
			public string CustomerInvoiceReference { get; set; }

			public int? PaymentMethodId { get; set; }

			public ProductVM[] Products { get; set; }
			// Navigational properties
			// Eventinfo is readonly
			public EventInfo EventInfo { get; set; }
		}
		#endregion
	}
}
