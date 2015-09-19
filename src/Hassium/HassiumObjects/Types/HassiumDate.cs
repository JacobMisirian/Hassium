﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using Hassium.Functions;

namespace Hassium.HassiumObjects.Types
{
    public class HassiumDate : HassiumObject
    {
        public DateTime Value { get; private set; }

        public HassiumDate(DateTime value)
        {
            Value = value;
            Attributes.Add("year", new InternalFunction(x => Value.Year, 0, true));
            Attributes.Add("month", new InternalFunction(x => Value.Month, 0, true));
            Attributes.Add("day", new InternalFunction(x => Value.Day, 0, true));
            Attributes.Add("hour", new InternalFunction(x => Value.Hour, 0, true));
            Attributes.Add("minute", new InternalFunction(x => Value.Minute, 0, true));
            Attributes.Add("second", new InternalFunction(x => Value.Second, 0, true));
            Attributes.Add("isLeapYear", new InternalFunction(x => DateTime.IsLeapYear(Value.Year), 0, true));
            Attributes.Add("timeStamp", new InternalFunction(x => GetTimestamp(new HassiumObject[] {}), 0, true));
            Attributes.Add("toString", new InternalFunction(toString, new[] {0, 1}));
        }

        public HassiumObject GetTimestamp(HassiumObject[] args)
        {
            return new HassiumInt((int) (Value - new DateTime(1970, 1, 1)).TotalSeconds);
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public HassiumObject toString(HassiumObject[] args)
        {
            if (args.Length == 0) return ToString();
            else
            {
                var final = new List<string>();
                var n = Value;
                foreach (var cur in args[0].ToString())
                {
                    string ta = "";
                    switch (cur)
                    {
                        case 'd':
                            ta = n.Day.ToString().PadLeft(2, '0');
                            break;
                        case 'D':
                            ta = n.DayOfWeek.ToString().Substring(0, 3);
                            break;
                        case 'j':
                            ta = n.Day.ToString();
                            break;
                        case 'l':
                            ta = n.DayOfWeek.ToString();
                            break;
                        case 'N':
                            ta = (int) (n.DayOfWeek + 6) % 7 + "";
                            break;
                        case 'w':
                            ta = (int) (n.DayOfWeek) + "";
                            break;
                        case 'z':
                            ta = n.DayOfYear.ToString();
                            break;
                        case 'W':
                            ta =
                                CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                                    n.AddDays(4 - ((int) (n.DayOfWeek + 6) % 7 == 0 ? 7 : (int) (n.DayOfWeek + 6) % 7)),
                                    CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday).ToString();
                            break;
                        case 'F':
                            ta = n.ToString("MMMM");
                            break;
                        case 'm':
                            ta = n.Month.ToString().PadLeft(2, '0');
                            break;
                        case 'M':
                            ta = n.ToString("MMM");
                            break;
                        case 'n':
                            ta = n.Month.ToString();
                            break;
                        case 't':
                            ta = new[] {31, 30, 28, 30, 31, 30, 31, 31, 30, 31, 30, 31}[n.Month - 1].ToString();
                            break;
                        case 'L':
                            ta = DateTime.IsLeapYear(n.Year) ? "1" : "0";
                            break;
                        case 'Y':
                            ta = n.Year.ToString();
                            break;
                        case 'y':
                            ta = n.Year.ToString().Substring(2, 2);
                            break;
                        case 'a':
                            ta = n.Hour > 12 ? "pm" : "am";
                            break;
                        case 'A':
                            ta = n.Hour > 12 ? "PM" : "AM";
                            break;
                        case 'p':
                            ta = n.Hour > 12 ? "p.m." : "a.m.";
                            break;
                        case 'P':
                            ta = n.Hour > 12 ? "P.M." : "A.M.";
                            break;
                        case 'B':
                            ta =
                                ((int) (n.ToUniversalTime().AddHours(1).TimeOfDay.TotalMilliseconds / 86400d)).ToString();
                            break;
                        case 'g':
                            ta = (n.Hour > 12 ? n.Hour - 12 : n.Hour).ToString();
                            break;
                        case 'G':
                            ta = n.Hour.ToString();
                            break;
                        case 'h':
                            ta = (n.Hour > 12 ? n.Hour - 12 : n.Hour).ToString().PadLeft(2, '0');
                            break;
                        case 'H':
                            ta = n.Hour.ToString().PadLeft(2, '0');
                            break;
                        case 'i':
                            ta = n.Minute.ToString().PadLeft(2, '0');
                            break;
                        case 's':
                            ta = n.Second.ToString().PadLeft(2, '0');
                            break;
                        case 'u':
                            ta = n.Millisecond * 1000 + "";
                            break;
                        case 'I':
                            ta = n.IsDaylightSavingTime() ? "1" : "0";
                            break;
                        case 'c':
                            var offset = TimeZoneInfo.Local.GetUtcOffset(n);
                            var offstr = "";
                            if (offset.ToString().StartsWith("-"))
                                offstr = "-" + offset.ToString().Replace(":", "").Substring(1, 4);
                            else
                                offstr = "+" + offset.ToString().Replace(":", "").Substring(0, 4);
                            ta = string.Format("{0}T{1}{2}", n.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                                n.ToString("HH:mm:ss", CultureInfo.InvariantCulture), offstr);
                            break;
                        case 'r':
                            ta = n.ToString("ddd, dd MMM yyyy hh:mm:ss ") + "GMT";
                            break;
                        case 'U':
                            ta = (int) (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds + "";
                            break;
                        default:
                            ta = cur.ToString();
                            break;
                    }
                    final.Add(ta);
                }
                return string.Join("", final);
            }
        }

        public static HassiumDate operator +(HassiumDate d1, HassiumDate d2)
        {
            var a = d1.Value;
            var b = d2.Value;
            var c =
                new HassiumDate(a + new TimeSpan(b.Year * 365 + b.DayOfYear, b.Hour, b.Minute, b.Second, b.Millisecond));
            return c;
        }

        public static HassiumDate operator -(HassiumDate d1, HassiumDate d2)
        {
            return new HassiumDate(new DateTime().Add(d1.Value - d2.Value));
        }
    }
}