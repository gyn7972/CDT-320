using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Configurations.Controls;
using MechaSys.SoftBricks.Parts;
using QMC.Parts.Assistant;

namespace QMC.Parts
{
    #region MaterialSizeManager
    public class MaterialSizeManager : Element
    {
        #region Field
        private MaterialSizeAssistantReadOnlyCollection m_Assistants;
        #endregion

        #region Constructor
        public MaterialSizeManager(Nameable nameable)
            : base(nameable)
        {

        }
        public MaterialSizeManager() : this(new Nameable()) { }
        #endregion

        #region Property
        public MaterialSizeAssistantReadOnlyCollection Assistants
        {
            get { return this.m_Assistants; }
            private set { this.m_Assistants = value; }
        }
        #endregion

        #region Method
        #endregion

        #region Element Members
        protected new MaterialSizeManagerConstructConfiguration ConstructConfiguration
        {
            get { return base.ConstructConfiguration as MaterialSizeManagerConstructConfiguration; }
        }

        protected override ElementConstructConfiguration OnGetDefaultConstructConfiguration()
        {
            return new MaterialSizeManagerConstructConfiguration();
        }

        protected override void OnSetConstructConfiguration(ElementConstructConfiguration configuration)
        {
            base.OnSetConstructConfiguration(configuration);

            if(this.ConstructConfiguration == null) return;
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            this.Assistants = new MaterialSizeAssistantReadOnlyCollection(this.Elements.GetByType<MaterialSizeAssistant>());
        }

        protected override void OnPrepare()
        {
            INeedMaterialSize[] elements = null;
            Dictionary<string, INeedMaterialSize> supportedElements = null;

            base.OnPrepare();

            elements = ElementList.GetByType<INeedMaterialSize>(true);
            supportedElements = new Dictionary<string, INeedMaterialSize>();

            for(int i = 0; i < this.Assistants.Count; i++)
            {
                for(int j = 0; j < this.Assistants[i].NeedMaterialSizeElements.Count; j++)
                {
                    if(supportedElements.ContainsKey(this.Assistants[i].NeedMaterialSizeElements[j].Locator.ToString()) == true)
                        throw new Exception("Size information can only be set in one place.");

                    supportedElements.Add(this.Assistants[i].NeedMaterialSizeElements[j].Locator.ToString(), this.Assistants[i].NeedMaterialSizeElements[j]);
                }
            }

            for(int i = 0; i < elements.Length; i++)
            {
                bool isContain = false;

                for(int j = 0; j < this.Assistants.Count; j++)
                {
                    if(this.Assistants[j].NeedMaterialSizeElements.ContainsByLocator(elements[i].Locator) == true)
                    {
                        elements[i].SetMaterialSizeSpecification(this.Assistants[j].MaterialSizeSpecifications);
                        elements[i].SelectSize(this.Assistants[j].SelectedSize);
                        isContain = true;
                        continue;
                    }
                }

                if(isContain == true) continue;

                elements[i].SetMaterialSizeSpecification(new MaterialSizeSpecificationKeyedCollection());
                elements[i].SelectSize(MaterialSizeSpecificationKeyedCollection.DefaultSize);
            }
        }
        #endregion
    }
    #endregion

    #region MaterialSizeManagerConstructConfiguration
    [Serializable]
    public class MaterialSizeManagerConstructConfiguration : ElementConstructConfiguration
    {
        #region Field

        #endregion

        #region Constructor
        public MaterialSizeManagerConstructConfiguration(ElementConstructMethod constructMethod)
            : base(ElementKind.Element, constructMethod)
        {
        }
        public MaterialSizeManagerConstructConfiguration() : this(ElementConstructMethod.Static) { }
        #endregion

        #region Property

        #endregion

        #region ConstructConfiguration Members
        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();
        }
        #endregion
    }
    #endregion
}
