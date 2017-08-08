using System;
using System.Windows.Input;

namespace St.Common
{


    public class LessonInfo
    {
        /// <summary>
        /// 课程编号
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 唯一编码
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 课程名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 课程介绍
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// 教室编号
        /// </summary>
        public int? ClassRoomId { get; set; }

        /// <summary>
        /// 学校编号
        /// </summary>
        public int SchoolId { get; set; }

        /// <summary>
        /// 联盟编号
        /// </summary>
        public int? SchoolGroupId { get; set; }

        /// <summary>
        /// 直播
        /// </summary>
        public bool Live { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 主讲设备
        /// </summary>
        public int MasterUserId { get; set; }

        /// <summary>
        /// 主讲人
        /// </summary>
        public string Teacher { get; set; }

        /// <summary>
        /// 讲课模式
        /// </summary>
        public StudyType? StudyType { get; set; }

        /// <summary>
        /// 主要知识点
        /// </summary>
        public string MainPoint { get; set; }

        public ICommand GotoLessonTypeCommand { get; set; }

        public LessonType LessonType { get; set; }

        public void CloneLessonInfo(LessonInfo newLessonInfo)
        {
            Id = newLessonInfo.Id;
            Code = newLessonInfo.Code;
            Name = newLessonInfo.Name;
            Summary = newLessonInfo.Summary;
            ClassRoomId = newLessonInfo.ClassRoomId;
            SchoolId = newLessonInfo.SchoolId;
            SchoolGroupId = newLessonInfo.SchoolGroupId;
            Live = newLessonInfo.Live;
            StartTime = newLessonInfo.StartTime;
            EndTime = newLessonInfo.EndTime;
            MasterUserId = newLessonInfo.MasterUserId;
            Teacher = newLessonInfo.Teacher;
            StudyType = newLessonInfo.StudyType;
            MainPoint = newLessonInfo.MainPoint;
            LessonType = newLessonInfo.LessonType;
        }
    }
}