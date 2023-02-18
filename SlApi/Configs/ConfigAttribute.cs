using AzyWorks.Configuration;

using System;

namespace SlApi.Configs
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ConfigAttribute : Attribute, IConfigAttribute
    {
        private string _name;
        private string _desc;

        public ConfigAttribute(string name, string description = null)
        {
            _name = name;
            _desc = description;
        }

        public string GetDescription()
            => _desc;
        public string GetName()
            => _name;
    }
}