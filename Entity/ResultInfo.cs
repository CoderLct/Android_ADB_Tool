using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Android_ADB_Tool
{
    public class ResultInfo
    {
        // CMD命令执行结果 0：成功，1：语法错误，-1：执行失败
        private int result = -1;

        //执行成功的情况下返回的数据
        private StreamReader data;

        public int getResult()
        {
            return this.result;
        }

        public void setResult(int result)
        {
            this.result = result;
        }
       
        public StreamReader getData()
        {
            return this.data;
        }

        public void setData(StreamReader data)
        {
            this.data = data;
        }
    }
}
