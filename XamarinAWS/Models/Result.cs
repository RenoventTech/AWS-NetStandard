using System;
using System.Collections.Generic;
using System.Text;

namespace XamarinAWS.Models
{
    public class Result<T>
    {
        public bool IsSuccess { get; set; }
        public List<string> Messages { get; set; }
        public T Return { get; set; }

        public Result()
        {
            this.Messages = new List<string>();
        }        

        /// <summary>
        /// Returns object with truthy success and optional messages
        /// </summary>
        /// <param name="returnObject"></param>
        /// <param name="messages">String array of messages</param>
        /// <returns></returns>
        public Result<T> AsSuccess(T returnObject, params string[] messages)
        {
            this.IsSuccess = true;
            this.Return = returnObject;

            this.Messages.AddRange(messages);

            return this;
        }

        /// <summary>
        /// Returns with a false success value and any passed error messages
        /// </summary>
        /// <param name="messages"></param>
        /// <returns></returns>
        public Result<T> AsError(params string[] messages)
        {
            this.IsSuccess = false;

            this.Messages.AddRange(messages);

            return this;
        }        
    }
}