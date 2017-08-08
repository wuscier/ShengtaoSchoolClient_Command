using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Caliburn.Micro;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using St.Common;

namespace St.Host.Core
{
    public class BmsService : IBms
    {
        public string AccessToken { get; set; }

        public async Task<ResponseResult> GetDateTime()
        {
            ResponseResult getServerTimeResult = await Request("/api/Extra/GetDateTime");

            return getServerTimeResult;
        }

        /// <summary>
        ///     获取访问令牌
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns>访问令牌</returns>
        public async Task<ResponseResult> ApplyForToken(string userName, string password, string deviceNo)
        {
            var bmsResult =
                await Request($"/api/Token/GetToken?username={userName}&password={password}&deviceid={deviceNo}");

            Log.Logger.Debug(
                $"【apply for token】：status={bmsResult.Status}, msg={bmsResult.Message}, data={bmsResult.Data}");

            if (bmsResult.Status == "-1")
            {
                bmsResult.Message = $"{Messages.WarningAuthenticationFailure}\n{bmsResult.Message}";
            }

            if (bmsResult.Data != null)
            {
                var data = bmsResult.Data as JObject;

                if (data != null) AccessToken = data.SelectToken("accessToken").ToString();
                bmsResult.Data = AccessToken;
            }

            return bmsResult;
        }

        public async Task<ResponseResult> GetUserInfo()
        {
            var bmsResult = await Request("/api/User/GetUserInfo");

            Log.Logger.Debug(
                $"【get user info】：status={bmsResult.Status}, msg={bmsResult.Message}, data={bmsResult.Data}");

            if (bmsResult.Data != null)
            {
                var data = bmsResult.Data as JObject;

                if (data != null)
                {
                    var jasonUser = data.SelectToken("user").ToString();

                    var userInfo = JsonConvert.DeserializeObject<UserInfo>(jasonUser);

                    var claims = data.SelectToken("claims") as JArray;

                    foreach (JObject jo in claims)
                        if (jo["name"].ToString() == "sub")
                        {
                            userInfo.OpenId = jo.SelectToken("value").ToString();
                            break;
                        }

                    bmsResult.Data = userInfo;
                }
            }

            return bmsResult;
        }

        public async Task<ResponseResult> GetDeviceInfo(string deviceNo, string deviceKey)
        {
            Log.Logger.Debug(
                $"【get device info from server begins】：deviceNo={deviceNo}, deviceKey={deviceKey}");

            ResponseResult deviceResult = await Request($"/api/User/GetDeviceInfo?Id={deviceNo}&Secret={deviceKey}");

            Log.Logger.Debug(
                $"【get device info from server result】：status={deviceResult.Status}, msg={deviceResult.Message}, data={deviceResult.Data}");

            if (deviceResult.Data != null)
            {
                var device = JsonConvert.DeserializeObject<Device>(deviceResult.Data.ToString());

                deviceResult.Data = device;
            }

            return deviceResult;
        }

        public async Task<ResponseResult> GetLessonTypes()
        {
            var bmsResult = await Request("/api/Awe/GetLessonTypes");

            if (bmsResult.Data != null)
            {
                var data = bmsResult.Data as JArray;
                var lessonTypes = new List<LessonType>();
                foreach (string lessonType in data)
                {
                    var lessonTypeEnum = (LessonType) Enum.Parse(typeof(LessonType), lessonType);
                    lessonTypes.Add(lessonTypeEnum);
                }

                bmsResult.Data = lessonTypes;
            }

            return bmsResult;
        }

        public async Task<ResponseResult> GetLessons(bool isExpired, LessonType? lessonType = null)
        {
            var bmsResult = await Request($"/api/Awe/GetLessons?LessonType={lessonType}&IsExpire={isExpired}");

            if (bmsResult.Data == null) return bmsResult;
            var data = bmsResult.Data as JObject;

            var jasonItems = data.SelectToken("items").ToString();

            var lessons = JsonConvert.DeserializeObject<List<LessonInfo>>(jasonItems);

            bmsResult.Data = lessons;

            return bmsResult;
        }

        public async Task<ResponseResult> GetLessonById(string lessonId)
        {
            var bmsResult = await Request($"/api/Awe/GetLessonById?id={lessonId}");

            var data = bmsResult.Data as JObject;
            if (data == null) return bmsResult;
            var lessonDetail = JsonConvert.DeserializeObject<LessonDetail>(data.ToString());

            bmsResult.Data = lessonDetail;

            return bmsResult;
        }

        public async Task<ResponseResult> GetUsersByLessonId(string lessonId)
        {
            var bmsResult = await Request($"/api/Awe/GetUsersByLessonId?lessonId={lessonId}");

            if (bmsResult.Data != null)
            {
                var data = bmsResult.Data as JObject;
                var jsonUsers = data.SelectToken("users").ToString();

                var users = JsonConvert.DeserializeObject<List<UserInfo>>(jsonUsers);

                bmsResult.Data = users;
            }

            return bmsResult;
        }

        public async Task<ResponseResult> GetMeetingByLessonId(string lessonId)
        {
            var bmsResult = await Request($"/api/Awe/GetMeetingByLessonId?lessonId={lessonId}");

            return bmsResult;
        }

        public async Task<ResponseResult> UpdateMeetingId(int lessonId, int meetingId)
        {
            JObject json = new JObject {{"lessonId", lessonId}, {"meetingId", meetingId}};

            HttpContent content = new StringContent(json.ToString());
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            ResponseResult result = await Request("/api/Awe/UpdateMeetingId", content);

            return result;
        }

        public async Task<ResponseResult> UpdateMeetingStatus(int lessonId, int userId, string enterTime,
            string exitTime)
        {
            JObject json = new JObject
            {
                {"lessonId", lessonId},
                {"userId", userId},
                {"enterTime", enterTime},
                {"exitTime", exitTime}
            };

            HttpContent content = new StringContent(json.ToString());
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            ResponseResult result = await Request("api/Awe/UpdateMeetingStatus", content);

            return result;
        }

        public async Task<ResponseResult> GetUserPic()
        {
            var bmsResult = await Request("/api/Awe/GetUserPic");
            return bmsResult;
        }

        public async Task<ResponseResult> UpdateUserPic(string content)
        {
            var obj = new {content};
            var httpContent = new StringContent(JsonConvert.SerializeObject(obj));
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var result = await Request("api/Awe/UpdateUserPic", httpContent);
            return result;
        }

        public async Task<ResponseResult> GetImeiToken(string deviceNo, string deviceKey)
        {
            //string encodedImei = Convert.ToBase64String(Encoding.UTF8.GetBytes(deviceNo));
            var bmsResult = await Request($"/api/Token/GetImeiToken?Id={deviceNo}&Secret={deviceKey}");

            //if (bmsResult.Status == "-1")
            //    bmsResult.Message = $"{Messages.WarningDeviceNotRegistered}\r\n设备号：{GlobalData.Instance.SerialNo}";

            Log.Logger.Debug(
                $"【get imei token】：status={bmsResult.Status}, msg={bmsResult.Message}, data={bmsResult.Data}");

            if (bmsResult.Data != null)
            {
                var data = bmsResult.Data as JObject;

                if (data != null) AccessToken = data.SelectToken("accessToken").ToString();
                bmsResult.Data = AccessToken;
            }

            return bmsResult;
        }

        public async Task<ResponseResult> Request(string url, HttpContent content = null)
        {
            var bmsResult = new ResponseResult();

            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(GlobalData.Instance.AggregatedConfig.GetInterfaceItem().BmsAddress);
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

                HttpResponseMessage response = null;
                try
                {
                    Log.Logger.Debug($"Request => url={url}");
                    response = content == null
                        ? await httpClient.GetAsync(url)
                        : await httpClient.PostAsync(url, content);

                    Log.Logger.Debug(
                        $"HttpResponseMessage => status={response.StatusCode}, message={response.ReasonPhrase}");
                }
                catch (Exception ex)
                {
                    bmsResult.Status = "-1";
                    bmsResult.Message = ex.Message;

                    Log.Logger.Error($"Request => {ex}");

                    return bmsResult;
                }

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();

                    var obj = JsonConvert.DeserializeObject(result, typeof(ResponseResult));
                    bmsResult = obj as ResponseResult;
                }
                else
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        Log.Logger.Debug($"【log out, because of timeout】：{response.StatusCode}");

                        IVisualizeShell visualizeShellService =
                            IoC.Get<IVisualizeShell>();
                        await visualizeShellService.Logout();
                    }

                    bmsResult.Message = response.ReasonPhrase;
                    bmsResult.Status = response.StatusCode.ToString();
                }

                return bmsResult;
            }
        }
    }
}