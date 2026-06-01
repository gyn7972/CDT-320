using System;
using System.Collections.Generic;
using System.ComponentModel;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Data;
using MechaSys.SoftBricks.Diagnostics;
using MechaSys.SoftBricks.DotNetUtility;
using MechaSys.SoftBricks.Exceptions;
using MechaSys.SoftBricks.Recipes;
using MechaSys.SoftBricks.Secs;

using SentiCore.Data.Serialization;

namespace QMC.Recipes
{
    #region ZipRecipeBodySecsItemConverter
    public class ZipRecipeBodySecsItemConverter : RecipeBodySecsItemConverter
    {
        #region Define
        [Serializable]
        public enum FormatCode
        {
            ASCII,
            Binary,
            Json,
        }
        #endregion

        #region Field
        #endregion

        #region Constructor
        public ZipRecipeBodySecsItemConverter(Nameable nameable)
            : base(nameable)
        {
        }
        public ZipRecipeBodySecsItemConverter() : this(new Nameable()) { }
        #endregion

        #region Property
        #endregion

        #region Method
        public virtual void ProcessJsonData(ref byte[] bytes)
        {
        }
        #endregion

        #region RecipeBodySecsItemConverter
        protected override RecipeBody OnFromSecsItem(SecsItem item)
        {
            RecipeBody body = null;
            string xml = "";
            byte[] bytes = null;

            if(this.ConstructConfiguration.FormatCode == FormatCode.ASCII)
            {
                xml = SecsItemConverter.ToString(item);
                xml = ZipUtility.Decompress(xml);
                body = XmlSerialization.Deserialize<RecipeBody>(xml);
            }
            else if(this.ConstructConfiguration.FormatCode == FormatCode.Binary)
            {
                bytes = SecsItemConverter.ToByteArray(item);
                bytes = ZipUtility.Decompress(bytes);
                body = BinarySerialization.Deserialize<RecipeBody>(bytes);
            }
            else if(this.ConstructConfiguration.FormatCode == FormatCode.Json)
            {
                bytes = SecsItemConverter.ToSecsBinary(bytes);
                bytes = ZipUtility.Decompress(bytes);
                NewtonSoftJsonSerializer.Deserialize < RecipeBody>(bytes, out body);
            }

            return body;
        }

        protected override SecsItem OnToSecsItem(RecipeBody body)
        {
            SecsItem item = null;
            string xml = "";
            byte[] bytes = null;

            if(this.ConstructConfiguration.FormatCode == FormatCode.ASCII)
            {
                xml = XmlSerialization.Serialize(body);
                xml = ZipUtility.Compress(xml);
                item = SecsItemConverter.ToSecsASCII(xml);
            }
            else if(this.ConstructConfiguration.FormatCode == FormatCode.Binary)
            {
                BinarySerialization.Serialize(ref bytes, body);
                bytes = ZipUtility.Compress(bytes);
                item = SecsItemConverter.ToSecsBinary(bytes);
            }
            else if(this.ConstructConfiguration.FormatCode == FormatCode.Json)
            {
                List<string> omitted = new List<string>();
                omitted.Add("Parent");
                omitted.Add("Verified");
                omitted.Add("ObjType");
                omitted.Add("Class");
                SentiCore.Data.Serialization.NewtonSoftJsonSerializer.Serialize(body, out bytes, omitted);
                this.ProcessJsonData(ref bytes);
                bytes = ZipUtility.Compress(bytes);
                item = SecsItemConverter.ToSecsBinary(bytes);
            }

            return item;
        }
        #endregion

        #region Element
        protected new ZipRecipeBodySecsItemConverterConstructConfiguration ConstructConfiguration
        {
            get { return base.ConstructConfiguration as ZipRecipeBodySecsItemConverterConstructConfiguration; }
        }

        protected override ElementConstructConfiguration OnGetDefaultConstructConfiguration()
        {
            return new ZipRecipeBodySecsItemConverterConstructConfiguration();
        }

        protected override void OnSetConstructConfiguration(ElementConstructConfiguration configuration)
        {
            base.OnSetConstructConfiguration(configuration);

            if(this.ConstructConfiguration == null) return;
        }
        #endregion
    }
    #endregion

    #region ZipRecipeBodySecsItemConverterConstructConfiguration
    [Serializable]
    public class ZipRecipeBodySecsItemConverterConstructConfiguration : RecipeBodySecsItemConverterConstructConfiguration
    {
        #region Field
        private ZipRecipeBodySecsItemConverter.FormatCode m_FormatCode;
        #endregion

        #region Constructor
        public ZipRecipeBodySecsItemConverterConstructConfiguration(ElementConstructMethod constructMethod)
            : base(constructMethod)
        {
        }
        public ZipRecipeBodySecsItemConverterConstructConfiguration() : this(ElementConstructMethod.Static) { }
        #endregion

        #region Property
        [Category("RecipeBodySecsItemConverter")]
        [DefaultValue(ZipRecipeBodySecsItemConverter.FormatCode.Binary)]
        public ZipRecipeBodySecsItemConverter.FormatCode FormatCode
        {
            get { return this.m_FormatCode; }
            set { this.m_FormatCode = value; }
        }
        #endregion

        #region ConstructConfiguration
        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();

            this.FormatCode = ZipRecipeBodySecsItemConverter.FormatCode.Binary;
        }
        #endregion
    }
    #endregion
}