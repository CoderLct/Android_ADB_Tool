using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Android_ADB_Tool
{
    public class ResultInfo<T>
    {
        public ResultInfo()
        {
            this.result = 1;
        }

        // CMD命令执行结果 0：成功，1：语法错误，-1：执行失败
        public int result { set; get; }

        /** 服务器消息 "成功"/... */
        public string message { set; get; }

        //执行成功的情况下返回的数据
        public T data { set; get;}
      
    }
}
