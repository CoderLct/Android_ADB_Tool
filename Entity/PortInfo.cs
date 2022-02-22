
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Android_ADB_Tool.Entity
{
    /**
     * 通道信息
     */
    class PortInfo
    {
        /*通道ID */
        public string portId { get; set; }
        /*通道名称 */
        public string portName { get; set; }
        /*通道类型 入口/出口 */
        public string portTypeName { get; set; }
        /*通道已绑定设备编号 */
        public string deviceCode { get; set; }
        /*设备类型 A3P/A1P/... */
        public string deviceType { get; set; }
        /*设备IP */
        public string portIp { get; set; }
        /*设备网关 */
        public string portGateway { get; set; }
        /*设备DNS */
        public string portDns { get; set; }
        /*相机1IP */
        public string cameraIp { get; set; }
        /*相机2IP */
        public string cameraIp2 { get; set; }
        /*机器人类型 AJB-NNN-A19/... */
        public string robotType { get; set; }
        /*机器人IP */
        public string robotIp { get; set; }
        /*机器人网关 */
        public string robotGateway { get; set; }
        /*机器人DNS */
        public string robotDns { get; set; }
        /*机器人ID */
        public string robotId { get; set; }
    }
}
