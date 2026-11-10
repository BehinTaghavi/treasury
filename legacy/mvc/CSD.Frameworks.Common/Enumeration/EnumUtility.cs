using System;
using System.Web;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Reflection;
using System.ComponentModel;

namespace CSD.Treasury.Common.Enumeration
{
    public class EnumUtility
    {
        public static List<SelectListItem> ToSelectList(Type enumType, string selectedItem = "", string ContainItemsValue = "", string ExceptItemsValue = "", bool AddEmptyRow = true)
        {
            List<SelectListItem> items = new List<SelectListItem>();
            SelectListItem selectedValue = null;
            if (AddEmptyRow)
                items.Add(new SelectListItem() { Text = " --- ", Value = "" });
            string[] ContainValues = ContainItemsValue.Split(',');
            string[] ExceptValues = ExceptItemsValue.Split(',');

            foreach (var item in Enum.GetValues(enumType))
            {
                string value = ((byte)item).ToString();
                if (string.IsNullOrEmpty(ExceptItemsValue) || ExceptValues.Length == 0 || ExceptValues.Where(u => u == value).Count() == 0)
                {
                    if (string.IsNullOrEmpty(ContainItemsValue) || ContainValues.Length == 0 || (ContainValues.Where(u => u == value).Count() > 0))
                    {
                        FieldInfo fi = enumType.GetField(item.ToString());
                        var attribute = fi.GetCustomAttributes(typeof(DescriptionAttribute), true).FirstOrDefault();
                        var title = attribute == null ? item.ToString() : ((DescriptionAttribute)attribute).Description;
                        var listItem = new SelectListItem
                        {
                            Value = value,
                            Text = TreasuryEnums.GetTitleFromResource(enumType.Name, title), //title,                            
                        };
                        if (selectedItem == ((byte)item).ToString())
                            listItem.Selected = true;
                        items.Add(listItem);
                        if (listItem.Selected)
                            selectedValue = listItem;
                    }
                }
            }

            return items;
        }

        public class EnumlistModel
        {
            public byte Id { get; set; }
            public string Title { get; set; }
        }

        public static List<EnumlistModel> ToList(Type enumType, string selectedItem = "", string ContainItemsValue = "", string ExceptItemsValue = "", bool AddEmptyRow = true)
        {
            List<EnumlistModel> items = new List<EnumlistModel>();
            if (AddEmptyRow)
                items.Add(new EnumlistModel() { Title = " --- " });

            string[] ContainValues = ContainItemsValue.Split(',');
            string[] ExceptValues = ExceptItemsValue.Split(',');

            foreach (var item in Enum.GetValues(enumType))
            {
                string value = ((byte)item).ToString();
                if (string.IsNullOrEmpty(ExceptItemsValue) || ExceptValues.Length == 0 || ExceptValues.Where(u => u == value).Count() == 0)
                {
                    if (string.IsNullOrEmpty(ContainItemsValue) || ContainValues.Length == 0 || (ContainValues.Where(u => u == value).Count() > 0))
                    {
                        FieldInfo fi = enumType.GetField(item.ToString());
                        var attribute = fi.GetCustomAttributes(typeof(DescriptionAttribute), true).FirstOrDefault();
                        var title = attribute == null ? item.ToString() : ((DescriptionAttribute)attribute).Description;
                        var listItem = new EnumlistModel
                        {
                            Id = byte.Parse(value),
                            Title = TreasuryEnums.GetTitleFromResource(enumType.Name, title)
                        };
                        items.Add(listItem);
                    }
                }
            }

            return items;
        }
    }
}
