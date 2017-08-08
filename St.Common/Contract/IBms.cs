using System.Threading.Tasks;

namespace St.Common
{
    public interface IBms
    {
        string AccessToken { get; }
        Task<ResponseResult> GetDateTime();
        Task<ResponseResult> ApplyForToken(string userName, string password, string deviceNo);
        Task<ResponseResult> GetUserInfo();
        Task<ResponseResult> GetDeviceInfo(string deviceNo, string deviceKey);
        Task<ResponseResult> GetLessonTypes();
        Task<ResponseResult> GetLessons(bool isExpired, LessonType? lessonType);
        Task<ResponseResult> GetLessonById(string lessonId);
        Task<ResponseResult> GetUsersByLessonId(string lessonId);
        Task<ResponseResult> GetMeetingByLessonId(string lessonId);
        Task<ResponseResult> UpdateMeetingId(int lessonId,int meetingId);
        Task<ResponseResult> UpdateMeetingStatus(int lessonId, int userId, string enterTime,
            string exitTime);

        Task<ResponseResult> GetImeiToken(string deviceNo, string deviceKey);
        Task<ResponseResult> GetUserPic();
        Task<ResponseResult> UpdateUserPic(string content);
    }
}
