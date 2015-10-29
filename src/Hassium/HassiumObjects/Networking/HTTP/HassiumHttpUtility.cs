﻿using System;
using System.Web;
using Hassium.Functions;
using Hassium.HassiumObjects.Types;

namespace Hassium.HassiumObjects.Networking.HTTP
{
    public class HassiumHttpUtility: HassiumObject
    {
        public HassiumHttpUtility()
        {
            Attributes.Add("htmlAttributesEncode", new InternalFunction(htmlAttributeEncode, 1));
            Attributes.Add("htmlDecode", new InternalFunction(htmlDecode, 1));
            Attributes.Add("htmlEncode", new InternalFunction(htmlEncode, 1));
            Attributes.Add("javaScriptStringEncode", new InternalFunction(javaScriptStringEncode, 1));
            Attributes.Add("urlDecode", new InternalFunction(urlDecode, 1));
            Attributes.Add("urlEncode", new InternalFunction(urlEncode, 1));
            Attributes.Add("addUriProtocol", new InternalFunction(addUriProtocol, 1));
        }

        public HassiumObject addUriProtocol(HassiumObject[] args)
        {
            try
            {
                return new UriBuilder(args[0].ToString()).Uri.ToString();
            }
            catch
            {
                throw new Exception("Invalid Uri");
            }
        }

        public HassiumObject htmlAttributeEncode(HassiumObject[] args)
        {
            return new HassiumString(HttpUtility.HtmlAttributeEncode(((HassiumString)args[0]).Value));
        }

        public HassiumObject htmlDecode(HassiumObject[] args)
        {
            return new HassiumString(HttpUtility.HtmlDecode(((HassiumString)args[0]).Value));
        }

        public HassiumObject htmlEncode(HassiumObject[] args)
        {
            return new HassiumString(HttpUtility.HtmlEncode(((HassiumString)args[0]).Value));
        }

        public HassiumObject javaScriptStringEncode(HassiumObject[] args)
        {
            return new HassiumString(HttpUtility.JavaScriptStringEncode(((HassiumString)args[0]).Value));
        }

        public HassiumObject urlDecode(HassiumObject[] args)
        {
            return new HassiumString(HttpUtility.UrlDecode(((HassiumString)args[0]).Value));
        }

        public HassiumObject urlEncode(HassiumObject[] args)
        {
            return new HassiumString(HttpUtility.UrlEncode(((HassiumString)args[0]).Value));
        }
    }
}
