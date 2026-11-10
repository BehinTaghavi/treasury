using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Windows.Forms;

namespace CSD.Common.Models
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class HideInFormAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class HideInListAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ShowInColumn2Attribute : Attribute
    {
    }

    public delegate string GetResourceText(string enumName,string key);

    public class EditFormModel
    {
        public int Id { get; set; }
        public int ColumnId { get; set; }

        public string Name { get; set; }
        public string DataTypeName { get; set; }
        public string DisplayText { get; set; }

        public bool IsEnum { get; set; }
        public bool IsNullable { get; set; }

        public Assembly PropertyAssembly { get; set; }
        public Type OriginDataType{ get; set; }
    }
    public class SelectItem
    {
        public string Text { get; set; }
        public object Value { get; set; }
        public string ValueText { get; set; }

        public override string ToString()
        {
            return this.Text;
        }
    }


    public class FormGenerating
    {
        public static string GetResourceString(Assembly resourceAssembly,string resourcename,string key )
        {
            
            try
            {
                var rm = new ResourceManager(resourcename, resourceAssembly);
                return rm.GetString(key);
            }
            catch(Exception ex)
            {
                //
            }
            return string.Empty;
        }

        public static bool HasThisAttribute(PropertyInfo p,Type attributeType)
        {
            var hp = p.GetCustomAttributes(attributeType, false);
            if (hp != null && hp.Length > 0)
                return true;
            return false;
        }

        public static string GetDisplayText(PropertyInfo p, Assembly resourceAssembly)
        {
            string DisplayText = string.Empty;
            
            #region CheckDisplayAttribute                        
            var at = p.GetCustomAttributes(typeof(DisplayAttribute), false);
            if (at != null && at.Length > 0)
            {
                var ao = at.FirstOrDefault();                
                DisplayText = ((DisplayAttribute)ao).Name;
                if (((DisplayAttribute)ao).ResourceType != null && !string.IsNullOrEmpty(((DisplayAttribute)ao).ResourceType.ToString()))
                {
                    DisplayText = GetResourceString(resourceAssembly, 
                        ((DisplayAttribute)ao).ResourceType.ToString(), ((DisplayAttribute)ao).Name);                    
                }
            }
            #endregion
            if (string.IsNullOrEmpty(DisplayText))
            {
                var atn = p.GetCustomAttributes(typeof(DisplayNameAttribute), false);
                if (atn != null && atn.Length > 0)
                {
                    var an = atn.FirstOrDefault();
                    DisplayText = ((DisplayNameAttribute)an).DisplayName;
                }
            }

            DisplayText = string.IsNullOrEmpty(DisplayText) ? p.Name : DisplayText;
            return DisplayText;
        }

        public static List<EditFormModel> GetSearchModelProperties<T>(
            Assembly resourceAssembly) where T : class
        {
            List<EditFormModel> efmList = new List<EditFormModel>();

            var pl = typeof(T).GetProperties(BindingFlags.Public |
                BindingFlags.Instance).Where(u => u.PropertyType.Namespace == "System").ToList();
            int i = 0;

            //Person_Annotation
            var mta = typeof(T).GetCustomAttributes(typeof(MetadataTypeAttribute), false); 
            //metadataClassType
            if (mta != null && mta.Length > 0)
            {
                var mt = mta.FirstOrDefault();
                var nT = ((MetadataTypeAttribute)mt).MetadataClassType;
            }

            foreach (var p in pl)
            {
                i++;

                var efm = new Models.EditFormModel() { Id = i, ColumnId = 1 };

                if (HasThisAttribute(p, typeof(HideInFormAttribute)))
                    continue;

                if (HasThisAttribute(p, typeof(CSD.Common.Models.ShowInColumn2Attribute)))
                {
                    efm.ColumnId = 2;
                }

                efm.DisplayText = GetDisplayText(p, resourceAssembly);

                efm.PropertyAssembly = p.PropertyType.Assembly;
                efm.OriginDataType = p.PropertyType;
                efm.Name = p.Name;
                efm.IsEnum = p.PropertyType.IsEnum;
                efm.DataTypeName = p.PropertyType.ToString();
                efm.IsNullable = efm.DataTypeName.ToLower().Contains("null");
                if (efm.IsNullable)
                {
                    var xd = p.PropertyType.UnderlyingSystemType.GetGenericArguments()[0];
                    efm.IsEnum = xd.IsEnum;
                    efm.DataTypeName = xd.Name;
                    efm.PropertyAssembly = xd.Assembly;
                    efm.OriginDataType = xd.UnderlyingSystemType;
                }
                efmList.Add(efm);

            }
            return efmList;
        }


        #region Control Creators

        public static Label CreateLabel(EditFormModel obj,Control parent,int top,int paddingLeft)
        {
            Label lb = new Label();
            lb.Text = obj.DisplayText;
            lb.Parent = parent;
            lb.Left = parent.Width - lb.Width - paddingLeft;
            lb.Top = top;
            return lb;
        }

        public static ComboBox CreateEnumCombo(EditFormModel obj, Control parent, int top, int comboLeft, int paddingLeft, IEnumerable<SelectItem> listItems)
        {
            ComboBox cb = new ComboBox();
            cb.Top = top;
            cb.Parent = parent;
            comboLeft -= cb.Width;
            cb.Left = comboLeft;
            comboLeft -= paddingLeft;
            cb.Name = string.Format("cb_{0}", obj.Name);
            foreach (var item in listItems)
            {
                cb.Items.Add(item);
            }
            return cb;
        }

        public static void CreateDateCombo()
        {
            /*
             * var cbl = parent.Width - paddingLeft - mxWidth;

                        ComboBox cb = new ComboBox();

                        cb.Top = t;
                        cb.Parent = parent;
                        cb.Name = string.Format("cb_day_{0}", obj.Name);
                        for ( i = 1; i < 32; i++)
                            cb.Items.Add(i);
                        
                        cb.Width = 45;
                        cbl -= cb.Width;
                        cb.Left = cbl ;
                        cbl -= paddingLeft;

                        cb = new ComboBox();

                        cb.Top = t;
                        cb.Parent = parent;
                        cb.Name = string.Format("cb_month_{0}", obj.Name);
                        for (i = 1; i < 13; i++)
                            cb.Items.Add(i);

                        cb.Width = 45;

                        cbl -= cb.Width;
                        cb.Left = cbl;
                        cbl -= paddingLeft;

                        cb = new ComboBox();

                        cb.Top = t;
                        cb.Parent = parent;
                        cb.Name = string.Format("cb_year_{0}", obj.Name);
                        for (i = 1; i < 13; i++)
                            cb.Items.Add(i);

                        cb.Width = 45;

                        cbl -= cb.Width;
                        cb.Left = cbl;
                        cbl -= paddingLeft;
             */
        }

        public static void EditorFor(EditFormModel obj, Control parent, int top, int paddingLeft, int defaultLineHeight, int mxWidth,GetResourceText rt)
        {

            if (obj.IsEnum)
            {
                var es = Enum.GetValues(obj.OriginDataType).OfType<Enum>();

                var listItems = Enum.GetValues(obj.OriginDataType).OfType<Enum>().Select(e => new SelectItem()
                {
                    Text = rt(obj.DataTypeName, e.ToString()),
                    Value = Enum.Parse(obj.OriginDataType, e.ToString()),
                    ValueText = e.ToString()
                });

                if (listItems.Count() < 5)
                {
                    var rbt = 0;
                    var rbl = parent.Width - paddingLeft - mxWidth;
                    foreach (var item in listItems)
                    {
                        RadioButton rb = new RadioButton();
                        rb.Text = item.Text;
                        rb.Tag = item.Value;
                        rb.Name = string.Format("rb_{0}_{1}", obj.DataTypeName, item.ValueText);
                        rb.Parent = parent;
                        rb.AutoSize = true;
                        rb.Top = top;
                        rbl -= rb.Width;
                        rb.Left = rbl;
                        rbl -= paddingLeft;
                        rbt = rb.Height;
                    }
                    top += rbt + defaultLineHeight;
                }
                else
                {
                    var cbl = parent.Width - paddingLeft - mxWidth;
                    ComboBox cb = CreateEnumCombo(obj, parent, top, cbl, paddingLeft, listItems);
                    top += cb.Height + defaultLineHeight;
                }
            }
            else
            {
                if (obj.DataTypeName.ToLower().Contains("date"))
                {
                    MaskedTextBox mtx = new MaskedTextBox();
                    mtx.Parent = parent;
                    mtx.Name = string.Format("txt_{0}", obj.Name);
                    mtx.Mask = "0000/00/00";
                    mtx.Left = parent.Width - paddingLeft - mxWidth - mtx.Width;
                    mtx.Top = top;
                    top += mtx.Height + defaultLineHeight;
                }
                else
                {
                    var txt = new TextBox();
                    txt.Name = string.Format("txt_{0}", obj.Name);
                    txt.Parent = parent;
                    txt.Left = parent.Width - txt.Width - paddingLeft - mxWidth;
                    txt.Top = top;
                    top += txt.Height + defaultLineHeight;
                }
            }
        }
        #endregion

        public static int EditorForModel(List<EditFormModel> efmList , 
            System.Windows.Forms.Control parent,
            Assembly enumResourceAssembly,
            GetResourceText rt,
            int defaultLineHeight=5 ,int paddingLeft=10, int defaultTop =20, int defaultMaxWidth=0,int c2Left=280,int c2Top= 20 )
        {
            var t = defaultTop;
            
            var mxWidth = defaultMaxWidth;
            var mx2Width = defaultMaxWidth;
            for (var i = 0; i < efmList.Count; i++)
            {
                var obj = efmList[i];
                if (obj.ColumnId == 1)
                {
                    Label lb = CreateLabel(obj, parent, t, paddingLeft);
                    if (lb.Width > mxWidth)
                        mxWidth = lb.Width;

                    EditorFor(obj, parent, t, paddingLeft, defaultLineHeight, mxWidth, rt);

                    t += lb.Height + defaultLineHeight;                  
                }
                else
                {
                    Label lb = CreateLabel(obj, parent, c2Top, paddingLeft+c2Left);
                    if (lb.Width > mx2Width)
                        mx2Width = lb.Width;

                    EditorFor(obj, parent, c2Top, paddingLeft+c2Left, defaultLineHeight, mx2Width, rt);

                    c2Top += lb.Height + defaultLineHeight;
                }
            }
            return t;                      
        }

    }


}
