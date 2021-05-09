using Eventuras.Domain;
using Eventuras.Services.Events;
using Eventuras.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Eventuras.WebApi.Controllers.Events
{
    [ApiVersion("3")]
    [Authorize(Policy = Constants.Auth.AdministratorRole)]
    [Route("v{version:apiVersion}/events")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly IEventInfoRetrievalService _eventInfoService;
        private readonly IEventManagementService _eventManagementService;

        public EventsController(
            IEventInfoRetrievalService eventInfoService,
            IEventManagementService eventManagementService)
        {
            _eventInfoService = eventInfoService ?? throw
                new ArgumentNullException(nameof(eventInfoService));

            _eventManagementService = eventManagementService ?? throw
                new ArgumentNullException(nameof(eventManagementService));
        }

        // GET: v3/events
        [AllowAnonymous]
        [HttpGet]
        public async Task<PageResponseDto<EventDto>> List(
            [FromQuery] EventsQueryDto query,
            CancellationToken cancellationToken)
        {
            var events = await _eventInfoService
                .ListEventsAsync(new EventListRequest(query.Offset, query.Limit)
                {
                    Filter = query.ToEventInfoFilter()
                }, cancellationToken: cancellationToken);

            return PageResponseDto<EventDto>.FromPaging(
                query, events, e => new EventDto(e));
        }

        // GET: v3/events/5
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<EventDto>> Get(int id, CancellationToken cancellationToken)
        {
            var eventInfo = await _eventInfoService.GetEventInfoByIdAsync(id, cancellationToken: cancellationToken);
            if (eventInfo == null || eventInfo.Archived)
            {
                return NotFound();
            }
            return Ok(new EventDto(eventInfo));
        }

        // POST: api/events
        [HttpPost]
        public async Task<EventDto> Post([FromBody] EventFormDto dto)
        {
            var eventInfo = new EventInfo();
            dto.CopyTo(eventInfo);
            await _eventManagementService.CreateNewEventAsync(eventInfo);
            return new EventDto(eventInfo);
        }

        // PUT: api/events/5
        [HttpPut("{id}")]
        public async Task<ActionResult<EventDto>> Put(int id, [FromBody] EventFormDto dto)
        {
            var eventInfo = await _eventInfoService.GetEventInfoByIdAsync(id);
            if (eventInfo.Archived)
            {
                return NotFound();
            }
            dto.CopyTo(eventInfo);
            await _eventManagementService.UpdateEventAsync(eventInfo);
            return Ok(new EventDto(eventInfo));
        }

        // PATCH: api/events/5
        [HttpPatch("{id}")]
        public async Task<IActionResult> JsonPatchWithModelState(
            int id,
            [FromBody] JsonPatchDocument<EventInfo> patchDoc)
        {
            if (patchDoc != null)
            {
                var eventInfo = await _eventInfoService.GetEventInfoByIdAsync(id);

                patchDoc.ApplyTo(eventInfo, ModelState);

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                await _eventManagementService.UpdateEventAsync(eventInfo);
                return Ok(new ObjectResult(eventInfo));
            }
            else
            {
                return BadRequest(ModelState);
            }
        }

        // DELETE: api/events/5
        [HttpDelete("{id}")]
        public async Task Delete(int id)
        {
            await _eventManagementService.DeleteEventAsync(id);
        }
    }
}
