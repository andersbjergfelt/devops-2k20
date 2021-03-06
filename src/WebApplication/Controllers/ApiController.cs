using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using WebApplication.Entities;
using WebApplication.Exceptions;
using WebApplication.Extensions;
using WebApplication.Helpers;
using WebApplication.Models.Api;
using WebApplication.Models.Authentication;
using WebApplication.Models.Timeline;
using WebApplication.ResponseModels;
using WebApplication.Services;

namespace WebApplication.Controllers
{
    [Route("api")]
    [ApiController]
    [Consumes("application/json")]
    [ActionLogger]
    public class ApiController : ControllerBase
    {
        private readonly DatabaseContext _databaseContext;
        private readonly TimelineService _timelineService;
        private readonly UserService _userService;

        public ApiController(DatabaseContext databaseContext, TimelineService timelineService, UserService userService)
        {
            _databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
            _timelineService = timelineService;
            _userService = userService;
        }

        /// <summary>
        /// Gets the ID of the latest request.
        /// </summary>
        [HttpGet("latest")]
        [ProducesResponseType(typeof(List<Latest>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLatest()
        {
            var latest = await TestingUtils.GetLatest(_databaseContext);
            
            return Ok(latest);
        }
        
        /// <summary>
        /// Registers a new user.
        /// </summary>
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddUser(RegisterModel model)
        {
            try
            {
                await _userService.CreateUser(model);
            }
            catch (CreateUserException exception)
            {
                return BadRequest(new ErrorResponse
                {
                    Status = 400,
                    Error = exception.Message
                });
            }

            return NoContent();
        }
        
        /// <summary>
        /// Gets public messages.
        /// </summary>
        /// <param name="no">The number of messages to return.</param>
        [HttpGet("msgs")]
        [ProducesResponseType(typeof(List<MessageResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<MessageResponse>>> GetMessages([FromQuery] int no = 20)
        {
            var messages = await _timelineService.GetMessagesForAnonymousUser(no, includeFlaggedMessages: false);

            return Ok(messages.Select(msg => new MessageResponse()
            {
                Content = msg.Text,
                PublishDate = msg.PublishDate,
                Author = msg.Author.Username
            }));
        }
        
        /// <summary>
        /// Gets message from the user with the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The username of the user to create the message for.</param>
        /// <param name="no">The number of messages to return.</param>
        [HttpGet("msgs/{username}")]
        [ProducesResponseType(typeof(List<MessageResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<MessageResponse>>> GetMessagesFromUser(string username, [FromQuery] int no = 20)
        {
            try
            {
                var messages = await _timelineService.GetMessagesForUser(username, no, includeFlaggedMessages: false);

                return Ok(messages.Select(msg => new MessageResponse
                {
                    Content = msg.Text,
                    PublishDate = msg.PublishDate,
                    Author = msg.Author.Username
                }));
            }
            catch (UnknownUserException e)
            {
                return BadRequest(new ErrorResponse(e));
            }
        }
        
        /// <summary>
        /// Creates a message for the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The username of the user to create the message for.</param>
        [HttpPost("msgs/{username}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddMessageToUser(string username, CreateMessageModel model)
        {
            try
            {
                await _timelineService.CreateMessage(model, username);
            }
            catch (UnknownUserException e)
            {
                return BadRequest(new ErrorResponse(e));
            }
            
            return NoContent();
        }
        
        /// <summary>
        /// Gets the followers of the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The username of the user to fetch followers for.</param>
        /// <param name="no">The number of followers to return.</param>
        /// <returns>Returns a collection of followers for the user.</returns>
        [HttpGet("fllws/{username}")]
        [ProducesResponseType(typeof(FollowerCollectionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<FollowerCollectionResponse>> GetFollowersFromUser(string username, [FromQuery] int no = 20)
        {
            try
            {
                var followers = await _userService.GetUserFollowers(username, no);

                return Ok(new FollowerCollectionResponse
                {
                    Follows = followers.Select(f => f.Who.Username).ToList()
                });
            }
            catch (UnknownUserException e)
            {
                return BadRequest(new ErrorResponse(e));
            }
        }
        
        /// <summary>
        /// Follows or un-follows the user with the specified <paramref name="username"/>.
        /// </summary>
        [HttpPost("fllws/{username}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddOrRemoveFollowerFromUser(string username, ChangeUserFollowerModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Follow) && string.IsNullOrWhiteSpace(model.Unfollow))
            {
                return BadRequest(new ErrorResponse
                {
                    Status = StatusCodes.Status400BadRequest,
                    Error = "Neither the user to follow or to unfollow as specified."
                });
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(model.Follow))
                {
                    await _userService.AddFollower(username, model.Follow);
                }
                else
                {
                    await _userService.RemoveFollower(username, model.Unfollow);
                }
            }
            catch (UnknownUserException e)
            {
                return BadRequest(new ErrorResponse(e));
            }
            catch (UnknownFollowerRelationException e)
            {
                return BadRequest(new ErrorResponse(e));
            }

            return NoContent();
        }
    }
}
