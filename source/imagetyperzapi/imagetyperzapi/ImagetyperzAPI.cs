﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.IO;

namespace imagetyperzapi
{
    public class ImagetyperzAPI
    {
        // consts
        private static string CAPTCHA_ENDPOINT = "http://captchatypers.com/Forms/UploadFileAndGetTextNEW.ashx";
        private static string RECAPTCHA_SUBMIT_ENDPOINT = "http://captchatypers.com/captchaapi/UploadRecaptchaV1.ashx";
        private static string RECAPTCHA_RETRIEVE_ENDPOINT = "http://captchatypers.com/captchaapi/GetRecaptchaText.ashx";
        private static string BALANCE_ENDPOINT = "http://captchatypers.com/Forms/RequestBalance.ashx";
        private static string BAD_IMAGE_ENDPOINT = "http://captchatypers.com/Forms/SetBadImage.ashx";

        private static string CAPTCHA_ENDPOINT_CONTENT_TOKEN = "http://captchatypers.com/Forms/UploadFileAndGetTextNEWToken.ashx";
        private static string CAPTCHA_ENDPOINT_URL_TOKEN = "http://captchatypers.com/Forms/FileUploadAndGetTextCaptchaURLToken.ashx";
        private static string RECAPTCHA_SUBMIT_ENDPOINT_TOKEN = "http://captchatypers.com/captchaapi/UploadRecaptchaToken.ashx";
        private static string RECAPTCHA_RETRIEVE_ENDPOINT_TOKEN = "http://captchatypers.com/captchaapi/GetRecaptchaTextToken.ashx";
        private static string BALANCE_ENDPOINT_TOKEN = "http://captchatypers.com/Forms/RequestBalanceToken.ashx";
        private static string BAD_IMAGE_ENDPOINT_TOKEN = "http://captchatypers.com/Forms/SetBadImageToken.ashx";
        private static string USER_AGENT = "csharpAPI1.0";      // user agent used in requests

        private string _access_token;
        private string _username = "";
        private string _password = "";
        private string _affiliateid;
        private int _timeout;

        private Captcha _captcha;
        private Recaptcha _recaptcha = null;

        private string _error = "";

        /// <summary>
        /// Initialize with access token
        /// </summary>
        /// <param name="access_token">access token</param>
        /// <param name="affiliate_id">affiliate id (optional)</param>
        /// <param name="timeout">timeout in seconds (optional)</param>
        public ImagetyperzAPI(string access_token, string affiliate_id = "0", int timeout = 120)
        {
            this._access_token = access_token;
            this._affiliateid = affiliate_id;
            this._timeout = timeout * 1000;
        }

        #region captcha & recaptcha
        /// <summary>
        /// Solve normal captcha
        /// </summary>
        /// <param name="captcha">captcha image file (local) or remote file (URL) [remote works only if tokens are used]</param>
        /// <param name="case_sensitive">case sensitive mode (true or false)</param>
        /// <returns></returns>
        public string solve_captcha(string captcha, bool case_sensitive = false)
        {
            string url, image_data = "";
            // use name value collection in this case
            var d = new Dictionary<string, string>();
            d.Add("action", "UPLOADCAPTCHA");
            d.Add("chkCase", (case_sensitive) ? "1" : "0");
            d.Add("affiliated", this._affiliateid);

            if (!string.IsNullOrWhiteSpace(this._username))
            {
                // legacy way
                d.Add("username", this._username);
                d.Add("password", this._password);
                url = CAPTCHA_ENDPOINT;
                // check if file/captcha image exists
                if (!File.Exists(captcha))
                {
                    throw new Exception(string.Format("captcha image file does not exist: {0}", captcha));
                }

                image_data = Utils.read_local_captcha(captcha);
            }
            else
            {
                // token way
                if (captcha.ToLower().StartsWith("http"))
                {
                    url = CAPTCHA_ENDPOINT_URL_TOKEN;
                    image_data = captcha;
                }
                else
                {
                    url = CAPTCHA_ENDPOINT_CONTENT_TOKEN;
                    if (!File.Exists(captcha))
                    {
                        throw new Exception(string.Format("captcha image file does not exist: {0}", captcha));
                    }
                    image_data = Utils.read_local_captcha(captcha);
                }
                d.Add("token", this._access_token);
            }

            // affiliate id
            if (!string.IsNullOrWhiteSpace(this._affiliateid) && this._affiliateid.ToString() != "0")
            {
                d.Add("affiliateid", this._affiliateid);
            }

            d.Add("file", image_data);

            // string fro dict
            var post_data = "";
            foreach (var e in d)
            {
                post_data += string.Format("{0}={1}&", e.Key, e.Value);
            }

            post_data = post_data.Substring(0, post_data.Length - 1);

            string response = Utils.POST(url, post_data, USER_AGENT, this._timeout);       // make request
            if (response.Contains("ERROR:"))
            {
                var response_err = response.Split(new string[] { "ERROR:" }, StringSplitOptions.None)[1].Trim();
                this._error = response_err;
                throw new Exception(response_err);
            }

            var c = new Captcha(response);      // create captcha obj
            this._captcha = c;
            return this._captcha.text();        // return captcha text
        }

        #region recaptcha
        /// <summary>
        /// Submit recaptcha and get it's captcha ID
        /// Check API docs for more info on how to get page_url and sitekey
        /// </summary>
        /// <param name="page_url">page url (check API docs)</param>
        /// <param name="sitekey">site key (check API docs)</param>
        /// <param name="proxy">IP:Port (optional)</param>
        /// <param name="proxy_type">ProxyType (optional)</param>
        /// <returns></returns>
        public string submit_recaptcha(string page_url, string sitekey, string proxy = "")
        {
            // check given vars
            if (string.IsNullOrWhiteSpace(page_url))
            {
                throw new Exception("page_url variable is null or empty");
            }
            if (string.IsNullOrWhiteSpace(sitekey))
            {
                throw new Exception("sitekey variable is null or empty");
            }

            string url = "";
            // create dict (params)
            Dictionary<string, string> d = new Dictionary<string, string>();
            d.Add("action", "UPLOADCAPTCHA");
            d.Add("pageurl", page_url);
            d.Add("googlekey", sitekey);

            // add proxy params if given
            if (!string.IsNullOrWhiteSpace(proxy))
            {
                d.Add("proxy", proxy);
            }

            if (!string.IsNullOrWhiteSpace(this._username))
            {
                d.Add("username", this._username);
                d.Add("password", this._password);
                url = RECAPTCHA_SUBMIT_ENDPOINT;
            }
            else
            {
                d.Add("token", this._access_token);
                url = RECAPTCHA_SUBMIT_ENDPOINT_TOKEN;
            }

            // affiliate id
            if (!string.IsNullOrWhiteSpace(this._affiliateid) && this._affiliateid.ToString() != "0")
            {
                d.Add("affiliateid", this._affiliateid);
            }

            var post_data = Utils.list_to_params(d);        // transform dict to params
            string response = Utils.POST(url, post_data, USER_AGENT, this._timeout);       // make request
            if (response.Contains("ERROR:"))
            {
                var response_err = response.Split(new string[] { "ERROR:" }, StringSplitOptions.None)[1].Trim();
                this._error = response_err;
                throw new Exception(response_err);
            }

            // set as recaptcha [id] response and return
            var r = new Recaptcha(response);
            this._recaptcha = r;
            return this._recaptcha.captcha_id();        // return captcha id
        }

        /// <summary>
        /// Retrieve recaptcha response using captcha ID
        /// </summary>
        /// <param name="captcha_id"></param>
        /// <returns></returns>
        public string retrieve_captcha(string captcha_id)
        {
            // check if ID is OK
            if (string.IsNullOrWhiteSpace(captcha_id))
            {
                throw new Exception("captcha ID is null or empty");
            }

            string url = "";
            // init params object
            Dictionary<string, string> d = new Dictionary<string, string>();
            d.Add("action", "GETTEXT");
            d.Add("captchaid", captcha_id);

            if (!string.IsNullOrWhiteSpace(this._username))
            {
                d.Add("username", this._username);
                d.Add("password", this._password);
                url = RECAPTCHA_RETRIEVE_ENDPOINT;
            }
            else
            {
                d.Add("token", this._access_token);
                url = RECAPTCHA_RETRIEVE_ENDPOINT_TOKEN;
            }

            var post_data = Utils.list_to_params(d);        // transform dict to params
            string response = Utils.POST(url, post_data, USER_AGENT, this._timeout);       // make request
            if (response.Contains("ERROR:"))
            {
                var response_err = response.Split(new string[] { "ERROR:" }, StringSplitOptions.None)[1].Trim();
                // in this case, if we get NOT_DECODED, we don't need it saved to obj
                // because it's used by bool in_progress(int captcha_id) as well
                if (!response_err.Contains("NOT_DECODED"))
                {
                    this._error = response_err;
                }
                throw new Exception(response_err);
            }

            // set as recaptcha response and return
            this._recaptcha = new Recaptcha(captcha_id);
            this._recaptcha.set_response(response);
            return this._recaptcha.response();        // return captcha text
        }

        /// <summary>
        /// Tells if the recaptcha is still in progress (still being decoded)
        /// </summary>
        /// <param name="captcha_id"></param>
        /// <returns></returns>
        public bool in_progress(string captcha_id)
        {
            try
            {
                this.retrieve_captcha(captcha_id);          // try to retrieve captcha
                return false;       // no error, we're good
            }
            catch (Exception ex)
            {
                // if "known" error, still in progress
                if (ex.Message.Contains("NOT_DECODED"))
                {
                    return true;
                }
                // otherwise throw exception (if different error)
                throw ex;
            }
        }
        #endregion
        #endregion

        #region others
        /// <summary>
        /// Get account balance
        /// </summary>
        /// <returns></returns>
        public string account_balance()
        {
            // create dict with params
            string url = "";
            Dictionary<string, string> d = new Dictionary<string, string>();
            d.Add("action", "REQUESTBALANCE");
            d.Add("submit", "Submit");

            if (!string.IsNullOrWhiteSpace(this._username))
            {
                d.Add("username", this._username);
                d.Add("password", this._password);
                url = BALANCE_ENDPOINT;
            }
            else
            {
                d.Add("token", this._access_token);
                url = BALANCE_ENDPOINT_TOKEN;
            }

            var post_data = Utils.list_to_params(d);        // transform dict to params
            string response = Utils.POST(url, post_data, USER_AGENT, this._timeout);       // make request
            if (response.Contains("ERROR:"))
            {
                var response_err = response.Split(new string[] { "ERROR:" }, StringSplitOptions.None)[1].Trim();
                this._error = response_err;
                throw new Exception(response_err);
            }

            return string.Format("${0}", response);        // return response/balance
        }

        /// <summary>
        /// Set captcha as bad
        /// </summary>
        /// <param name="captcha_id"></param>
        /// <returns></returns>
        public string set_captcha_bad(string captcha_id)
        {
            // check if captcha is OK
            if (string.IsNullOrWhiteSpace(captcha_id))
            {
                throw new Exception("catpcha ID is null or empty");
            }

            string url = "";
            // create dict with params
            Dictionary<string, string> d = new Dictionary<string, string>();
            d.Add("action", "SETBADIMAGE");
            d.Add("imageid", captcha_id);
            d.Add("submit", "Submissssst");

            if (!string.IsNullOrWhiteSpace(this._username))
            {
                d.Add("username", this._username);
                d.Add("password", this._password);
                url = BAD_IMAGE_ENDPOINT;
            }
            else
            {
                d.Add("token", this._access_token);
                url = BAD_IMAGE_ENDPOINT_TOKEN;
            }

            var post_data = Utils.list_to_params(d);        // transform dict to params
            string response = Utils.POST(url, post_data, USER_AGENT, this._timeout);       // make request
            if (response.Contains("ERROR:"))
            {
                var response_err = response.Split(new string[] { "ERROR:" }, StringSplitOptions.None)[1].Trim();
                this._error = response_err;
                throw new Exception(response_err);
            }

            return response;
        }
        #endregion

        #region misc
        /// <summary>
        /// Legacy - use user and password instead of token. will be deprecated at some point, use access token
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        public void set_user_and_password(string user, string password)
        {
            this._username = user;
            this._password = password;
        }
        /// <summary>
        /// Set timeout (in seconds)
        /// </summary>
        /// <param name="timeout"></param>
        public void set_timeout(int timeout)
        {
            this._timeout = timeout * 1000;     // requests timeout is in milliseconds, multiply by 1k
        }

        /// <summary>
        /// Get last solved captcha text
        /// </summary>
        /// <returns></returns>
        public string captcha_text()
        {
            if (this._captcha == null)      // check if captcha is null
            {
                return "";
            }
            return this._captcha.text();        // return last solved captcha text
        }
        /// <summary>
        /// Get last solved captcha id
        /// </summary>
        /// <returns></returns>
        public string captcha_id()
        {
            if (this._captcha == null)      // check if captcha is null
            {
                return "";
            }
            return this._captcha.captcha_id();        // return last solved captcha text
        }

        /// <summary>
        /// Get last solved recaptcha response
        /// </summary>
        /// <returns></returns>
        public string recaptcha_response()
        {
            if (this._recaptcha == null)
            {
                return "";
            }
            return this._recaptcha.response();      // return response
        }
        /// <summary>
        /// Get last solved recaptcha id
        /// </summary>
        /// <returns></returns>
        public string recaptcha_id()
        {
            if (this._recaptcha == null)        // check if recaptcha is null
            {
                return "";
            }
            return this._recaptcha.captcha_id();        // return recaptcha id
        }

        /// <summary>
        /// Get last error
        /// </summary>
        /// <returns></returns>
        public string error()
        {
            return this._error;
        }
        #endregion
    }
}
