﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using losol.EventManagement.Domain;

namespace losol.EventManagement.Services
{
	public interface IEventInfoService
	{
		Task<EventInfo> GetAsync(int id);
		Task<EventInfo> GetWithProductsAsync(int id);
		Task<int> GetVerifiedRegistrationCount(int eventId);

		Task<List<EventInfo>> GetFeaturedEventsAsync();
		Task<List<EventInfo>> GetEventsAsync();
		Task<List<EventInfo>> GetOnDemandEventsAsync();

		Task<bool> AddAsync(EventInfo info);
		Task<bool> UpdateEventWithProductsAsync(EventInfo info);
	}
}
