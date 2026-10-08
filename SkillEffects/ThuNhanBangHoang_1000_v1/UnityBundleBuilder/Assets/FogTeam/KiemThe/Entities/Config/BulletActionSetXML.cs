using System;
using System.Collections.Generic;

namespace FogTeam.KiemThe.Entities.Config
{
    /// <summary>
    /// Đối tượng quản lý Res đạn
    /// </summary>
    [Serializable]
    public class BulletActionSetXML
    {
        /// <summary>
        /// Đường dẫn file Bundle chứa âm thanh hiệu ứng đạn
        /// </summary>
        public string SoundBundleDir ;

        /// <summary>
        /// Đối tượng dữ liệu Res đạn
        /// </summary>
        [Serializable]
        public class BulletResData
        {
            /// <summary>
            /// ID Res
            /// </summary>
            public int ResID ;

            /// <summary>
            /// Tên Res
            /// </summary>
            public string Name ;

            /// <summary>
            /// Đường dẫn file Bundle chứa ảnh hiệu ứng đạn
            /// </summary>
            public string BundleDir ;

            /// <summary>
            /// Có động tác bay
            /// </summary>
            public bool HasFlyAction ;

            /// <summary>
            /// Có động tác tan
            /// </summary>
            public bool HasFadeOutAction ;

            /// <summary>
            /// Có hiệu ứng nổ
            /// </summary>
            public bool HasExplodeAction ;

            /// <summary>
            /// Sử dụng 18 hướng không
            /// </summary>
            public bool Use16Dir ;

            /// <summary>
            /// Tự động quay (nếu sử dụng 16 hướng thì lựa chọn này không có tác dụng)
            /// </summary>
            public bool AutoRotate ;

            /// <summary>
            /// Sử dụng hiệu ứng tạo bóng di chuyển
            /// </summary>
            public bool UseTrailEffect ;
            
            /// <summary>
            /// Thời gian duy trì bóng
            /// </summary>
            public float TrailDuration ;

            /// <summary>
            /// Thời gian giãn cách mỗi lần tạo bóng
            /// </summary>
            public float TrailPeriod ;
        }

        /// <summary>
        /// Danh sách hiệu ứng theo ID Res
        /// </summary>
        public Dictionary<int, BulletResData> ResDatas ;
    }
}
