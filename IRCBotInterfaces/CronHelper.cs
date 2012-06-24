using System;
//using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace IRCBotInterfaces
{
    internal class CronHelper
    {
        //http://de.wikibooks.org/wiki/Linux-Kompendium:_crontab

        private static readonly int[] minValues = { 0, 0, 0, 1, 0 };
        private static readonly int[] maxValues = { 59, 23, 31, 12, 7 };

        public static bool Match(DateTime dt, string cronDefinition)
        {
            var results = Parse(cronDefinition);

            bool valid = true;
            valid &= results[0].Matches(dt.Minute);
            valid &= results[1].Matches(dt.Hour);
            valid &= results[2].Matches(dt.Day);
            valid &= results[3].Matches(dt.Month);
            valid &= CheckDayOfWeek(results[4], dt.DayOfWeek);

            return valid;
        }

        public static void Check(string cronDefinition)
        {
            Parse(cronDefinition);
        }

        private static IPartResult[] Parse(string cronDefinition)
        {
            var cronParts = cronDefinition.Split(' ');

            if (cronParts.Length != 5)
            {
                throw new InvalidOperationException("Invalid cron syntax: Not exactly 5 parts given");
            }

            var results = new IPartResult[5];
            for (int i = 0; i < cronParts.Length; i++)
            {
                var res = GetPartResult(cronParts[i].Trim(), minValues[i], maxValues[i]);
                if (res == null)
                {
                    throw new InvalidOperationException("Invalid cron syntax: Could not parse parameter " + i);
                }
                results[i] = res;
            }
            return results;
        }

        private static bool CheckDayOfWeek(IPartResult partResult, DayOfWeek dayOfWeek)
        {
            if (dayOfWeek == DayOfWeek.Sunday)
            {
                return partResult.Matches(0) || partResult.Matches(7);
            }
            else
            {
                return partResult.Matches((int)dayOfWeek);
            }
        }

        private static IPartResult GetPartResult(string part, int minValue, int maxValue)
        {
            if (part == "*")
            {
                return new PartResultStar();
            }
            else if (part.StartsWith("*/") && part.Length >= 3)
            {
                try
                {
                    int val = Int32.Parse(part.Substring(2));

                    if (val < minValue)
                        val = minValue;
                    if (val > maxValue)
                        val = maxValue;
                    if (val == 0 || val == 1)
                    {
                        return new PartResultStar();
                    }
                    else
                    {
                        return new PartResultStep(val);
                    }
                }
                catch (FormatException)
                {
                    return null;
                }
            }
            else if (part.Contains(","))
            {
                if (part.Contains("-"))
                {
                    var list = new List<PartResultRange>();
                    var subParts = part.Split(',');
                    foreach (var subPart in subParts)
                    {
                        if (subPart.Contains("-"))
                        {
                            var prr = GetPartRangeResult(minValue, maxValue, subPart, -1) as PartResultRange;
                            if (prr != null)
                            {
                                list.Add(prr);
                            }
                        }
                        else
                        {
                            try
                            {
                                int a = Int32.Parse(subPart);
                                list.Add(new PartResultRange(a, a));
                            }
                            catch (FormatException)
                            {
                            }
                        }
                    }
                    return new PartResultRangeList(list);
                }
                else
                {
                    var list = new List<int>();
                    var subParts = part.Split(',');
                    foreach (var subPart in subParts)
                    {
                        try
                        {
                            int a = Int32.Parse(subPart);
                            list.Add(a);
                        }
                        catch (FormatException)
                        {
                        }
                    }
                    return new PartResultList(list);
                }
            }
            else if (part.Contains("-"))
            {
                var part2 = part;
                int step = -1;
                if (part.Contains("/"))
                {
                    var stepParts = part.Split('/');
                    if (stepParts.Length != 2)
                    {
                        return null;
                    }

                    part2 = stepParts[0];

                    try
                    {
                        step = Int32.Parse(stepParts[1]);

                        if (step < minValue)
                            step = minValue;
                        if (step > maxValue)
                            step = maxValue;

                        // 'A-B/0' or 'A-B/1' will be interpreted as 'A-B'
                        if (step <= 1)
                            step = -1;
                    }
                    catch (FormatException)
                    {
                        return null;
                    }
                }

                return GetPartRangeResult(minValue, maxValue, part2, step);
            }
            else
            {
                try
                {
                    int val = Int32.Parse(part);

                    return new PartResultValue(val);
                }
                catch (FormatException)
                {
                    return null;
                }
            }
        }

        private static IPartResult GetPartRangeResult(int minValue, int maxValue, string part, int step)
        {
            var subParts = part.Split('-');
            if (subParts.Length == 2)
            {
                try
                {
                    int min = Int32.Parse(subParts[0]);
                    int max = Int32.Parse(subParts[1]);

                    if (min > max)
                    {
                        return null;
                    }

                    min = min > minValue ? min : minValue;
                    max = max < maxValue ? max : maxValue;

                    if (step >= 1)
                    {
                        return new PartResultRangeWithStep(min, max, step);
                    }
                    else
                    {
                        return new PartResultRange(min, max);
                    }
                }
                catch (FormatException)
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        private interface IPartResult
        {
            bool Matches(int value);
        }

        private class PartResultStar : IPartResult
        {
            #region IPartResult Members

            public bool Matches(int value)
            {
                return true;
            }

            #endregion
        }

        private class PartResultValue : IPartResult
        {
            private int m_value;

            public PartResultValue(int value)
            {
                this.m_value = value;
            }

            #region IPartResult Members

            public bool Matches(int value)
            {
                return value == m_value;
            }

            #endregion
        }

        private class PartResultRange : IPartResult
        {
            private int m_min;
            private int m_max;

            public PartResultRange(int min, int max)
            {
                m_max = max;
                m_min = min;
            }

            #region IPartResult Members

            public virtual bool Matches(int value)
            {
                return m_min <= value && m_max >= value;
            }

            #endregion
        }

        private class PartResultList : IPartResult
        {
            private List<int> m_values;

            public PartResultList(List<int> values)
            {
                m_values = values;
            }

            #region IPartResult Members

            public bool Matches(int value)
            {
                return m_values.Contains(value);
            }

            #endregion
        }

        private class PartResultStep : IPartResult
        {
            private int m_step;

            public PartResultStep(int step)
            {
                this.m_step = step;
            }
            #region IPartResult Members

            public bool Matches(int value)
            {
                return value % m_step == 0;
            }

            #endregion
        }

        private class PartResultRangeWithStep : PartResultRange
        {
            private int m_step;

            public PartResultRangeWithStep(int min, int max, int step)
                : base(min, max)
            {
                m_step = step;
            }

            public override bool Matches(int value)
            {
                if (base.Matches(value))
                {
                    return value % m_step == 0;
                }
                else
                {
                    return false;
                }
            }
        }

        private class PartResultRangeList : IPartResult
        {
            private List<PartResultRange> m_ranges;

            public PartResultRangeList(List<PartResultRange> ranges)
            {
                m_ranges = ranges;
            }

            #region IPartResult Members

            public bool Matches(int value)
            {
                foreach (var range in m_ranges)
                {
                    if (range.Matches(value))
                    {
                        return true;
                    }
                }
                return false;
            }

            #endregion
        }

    }
}
