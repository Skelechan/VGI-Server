using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Interfaces;
using vgi.Server.Models;

namespace VGI.Server.Controllers
{
    [ApiController]
    [Route("api/twitch")]
    public class TwitchController(ITwitchAPI twitchApi, IConfiguration configuration) : ControllerBase
    {
        private readonly List<MemberInformation> _memberInformation = configuration.GetSection("MemberInformation").Get<List<MemberInformation>>();


        [HttpGet("streams")]
        public async Task<IEnumerable<TwitchStream>> GetUserInformation()
        {
            var twitchIds = _memberInformation.Select(x => x.TwitchId).ToList();

            var liveStreamsResponse = await twitchApi.Helix.Streams.GetStreamsAsync(userIds: twitchIds);
            var userInfo = await twitchApi.Helix.Users.GetUsersAsync(ids: twitchIds);
            var userColor = await twitchApi.Helix.Chat.GetUserChatColorAsync(userIds: twitchIds);

            var mappedItems = liveStreamsResponse.Streams.Select(stream =>
            {
                var foundUser = userInfo.Users.Single(user => user.Id == stream.UserId);
                var foundColor = userColor.Data.Single(user => user.UserId == stream.UserId);
                return new TwitchStream
                {
                    GameName = stream.GameName,
                    DisplayName = foundUser.DisplayName,
                    StreamThumbnail = stream.ThumbnailUrl.Replace("{height}", "480").Replace("{width}", "640"),
                    ProfileImage = foundUser.ProfileImageUrl,
                    ProfileColor = foundColor.Color
                };
            });
            
            return mappedItems;
        }
        
        [HttpGet("vods")]
        public async Task<IEnumerable<TwitchVideo>> GetVods([FromQuery] int size = 10)
        {
            var vods = new ConcurrentBag<TwitchVideo>();
            
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 8 };
            await Parallel.ForEachAsync(_memberInformation.Select(x => x.TwitchId), parallelOptions, async (member, _) =>
            {
                var liveStreamsResponse = await twitchApi.Helix.Videos.GetVideosAsync(userId: member, type: VideoType.Archive, period: Period.Week, first: size);
                var userInfo = await twitchApi.Helix.Users.GetUsersAsync(ids: new List<string>{member});
                var castVideos = liveStreamsResponse.Videos.Select(vod =>
                {
                    var foundUser = userInfo.Users.Single(user => user.Id == vod.UserId);
                    return new TwitchVideo
                    {
                        Title = vod.Title,
                        StreamThumbnail = vod.ThumbnailUrl.Replace("{height}", "300").Replace("{width}", "300"),
                        DisplayName = foundUser.DisplayName,
                        CreationDate = vod.CreatedAt,
                        Url = vod.Url
                    };
                });
                    
                foreach (var video in castVideos)
                    vods.Add(video);
            });

            return vods.OrderByDescending(x => x.CreationDate).Take(size);
        }
        
        [HttpGet("clips")]
        public async Task<IEnumerable<TwitchVideo>> GetClips([FromQuery] int size = 10)
        {
            var vods = new ConcurrentBag<TwitchVideo>();
            
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 8 };
            await Parallel.ForEachAsync(_memberInformation.Select(x => x.TwitchId), parallelOptions, async (member, _) =>
            {
                var liveStreamsResponse = await twitchApi.Helix.Clips.GetClipsAsync(broadcasterId: member, startedAt: DateTime.Today.AddDays(-7), first: size);
                var userInfo = await twitchApi.Helix.Users.GetUsersAsync(ids: new List<string>{member});
                var castVideos = liveStreamsResponse.Clips.Select(clip =>
                {
                    var foundUser = userInfo.Users.Single(user => user.Id == clip.BroadcasterId);
                    return new TwitchVideo
                    {
                        Title = clip.Title,
                        StreamThumbnail = clip.ThumbnailUrl.Replace("{height}", "480").Replace("{width}", "640"),
                        DisplayName = foundUser.DisplayName,
                        Url = clip.Url,
                        CreationDate = clip.CreatedAt
                    };
                });
                    
                foreach (var video in castVideos)
                    vods.Add(video);
            });

            return vods.OrderByDescending(x => x.CreationDate).Take(size);
        }
    }
}