using System;

using MechaSys.SoftBricks;
using MechaSys.SoftBricks.Configurations;
using MechaSys.SoftBricks.Diagnostics;
using MechaSys.SoftBricks.Exceptions;
using MechaSys.SoftBricks.Jobs;
using MechaSys.SoftBricks.Materials;
using MechaSys.SoftBricks.ObjectServices;
using MechaSys.SoftBricks.Recipes;
using MechaSys.SoftBricks.Secs;
using MechaSys.SoftBricks.Secs.Message;
using MechaSys.SoftBricks.Secs.Message.Specialized;

namespace QMC.Secs
{
    #region GeneralGemService
    public class GeneralGemService : GemService
    {
        #region Field
        #endregion

        #region Constructor
        public GeneralGemService(Nameable nameable)
            : base(nameable)
        {
        }
        public GeneralGemService() : this(new Nameable()) { }
        #endregion

        #region Property
        #endregion

        #region Method
        protected virtual void SetRemoteCommand()
        {
            RemoteCommandSpecification command = null;
            DirectObjectServiceConfiguration direct = null;

            command = new RemoteCommandSpecification(nameof(this.ChangeControlToOnLineLocal));
            command.ServiceMethod = ObjectServiceMethod.Direct;
            direct = new DirectObjectServiceConfiguration();
            direct.Target = this;
            direct.ServiceName = nameof(this.ChangeControlToOnLineLocal);
            command.DirectConfiguration = direct;
            this.RemoteCommandSpecifications.Add(command);

            command = new RemoteCommandSpecification(nameof(this.ChangeControlToOnLineRemote));
            command.ServiceMethod = ObjectServiceMethod.Direct;
            direct = new DirectObjectServiceConfiguration();
            direct.Target = this;
            direct.ServiceName = nameof(this.ChangeControlToOnLineRemote);
            command.DirectConfiguration = direct;
            this.RemoteCommandSpecifications.Add(command);

            command = new RemoteCommandSpecification(nameof(this.SelectProcessProgram));
            command.ServiceMethod = ObjectServiceMethod.Direct;
            direct = new DirectObjectServiceConfiguration();
            direct.Target = this;
            direct.ServiceName = nameof(this.SelectProcessProgram);
            command.DirectConfiguration = direct;
            this.RemoteCommandSpecifications.Add(command);

            command = new RemoteCommandSpecification(nameof(this.CreateControlJob));
            command.ServiceMethod = ObjectServiceMethod.Direct;
            direct = new DirectObjectServiceConfiguration();
            direct.Target = this;
            direct.ServiceName = nameof(this.CreateControlJob);
            command.DirectConfiguration = direct;
            this.RemoteCommandSpecifications.Add(command);

            command = new RemoteCommandSpecification(nameof(this.StartControlJob));
            command.ServiceMethod = ObjectServiceMethod.Direct;
            direct = new DirectObjectServiceConfiguration();
            direct.Target = this;
            direct.ServiceName = nameof(this.StartControlJob);
            command.DirectConfiguration = direct;
            this.RemoteCommandSpecifications.Add(command);

            command = new RemoteCommandSpecification(nameof(this.PauseControlJob));
            command.ServiceMethod = ObjectServiceMethod.Direct;
            direct = new DirectObjectServiceConfiguration();
            direct.Target = this;
            direct.ServiceName = nameof(this.PauseControlJob);
            command.DirectConfiguration = direct;
            this.RemoteCommandSpecifications.Add(command);

            command = new RemoteCommandSpecification(nameof(this.ResumeControlJob));
            command.ServiceMethod = ObjectServiceMethod.Direct;
            direct = new DirectObjectServiceConfiguration();
            direct.Target = this;
            direct.ServiceName = nameof(this.ResumeControlJob);
            command.DirectConfiguration = direct;
            this.RemoteCommandSpecifications.Add(command);

            command = new RemoteCommandSpecification(nameof(this.StopControlJob));
            command.ServiceMethod = ObjectServiceMethod.Direct;
            direct = new DirectObjectServiceConfiguration();
            direct.Target = this;
            direct.ServiceName = nameof(this.StopControlJob);
            command.DirectConfiguration = direct;
            this.RemoteCommandSpecifications.Add(command);
        }

        public int SelectProcessProgram(string recipeid)
        {
            int ret = 0;

            if (UnionRecipeAssigner.Instances == null || UnionRecipeAssigner.Instances.Count == 0)
                return ErrorManager.Register("Union Recipe Assigner is not exist");

            if((ret = UnionRecipeAssigner.Instances[0].Assign(recipeid))!=0)
            {
                Error error = ErrorManager.GetByUid(ret);
                if (error != null)
                    Log.Write(this, new LogEntry(LogLevel.Highest, string.Format("UnionRecipeAssigner.Assign() failed : {0}", error.ToString())));
                else
                    Log.Write(this, new LogEntry(LogLevel.Highest, string.Format("UnionRecipeAssigner.Assign() failed : {0}", "Unknown")));
                return ret;
            }

            return ret;
        }

        public int CreateControlJob(string jobid, string recipeid, string carrierid)
        {
            int ret = 0;

            if ((ret = this.OnCreateControlJob(jobid, recipeid, carrierid)) == 0) return ret;

            return ret;
        }

        public int CreateControlJob(string jobid, string recipeid, string carrierid1, string carrierid2)
        {
            int ret = 0;
            StringCollection list = new StringCollection();
            string[] carrierIds;

            if (string.IsNullOrEmpty(carrierid1) == false)
                list.Add(carrierid1);
            if (string.IsNullOrEmpty(carrierid2) == false)
                list.Add(carrierid2);

            carrierIds = list.ToArray();
            if ((ret = this.OnCreateControlJob(jobid, recipeid, carrierIds)) == 0) return ret;

            return ret;
        }

        protected virtual int OnCreateControlJob(string jobid, string recipeid, params string[] carrierids)
        {
            int ret = 0;
            ProcessJobAttribute processJobAttribute;
            ControlJobAttribute controlJobAttribute;
            StringCollection processJobObjIds;
            ProcessJob processJob;
            ControlJob controlJob;
            CarrierCollection carriers;
            Carrier carrier;
            string text;
            RecipeIdentifier recipeIdentifier;
            ManagedRecipe recipe;

            #region check arguments
            carriers = new CarrierCollection();
            foreach (string carrierid in carrierids)
            {
                carrier = CarrierManager.GetByObjId(carrierid);
                if (carrier == null)
                {
                    text = string.Format("Carrier [{0}] does not exist.", carrierid);
                    this.WriteLog(LogLevel.Highest, text);
                    ret = ErrorManager.Register(text);
                    return ret;
                }
                carriers.Add(carrier);
            }

            if (RecipeIdentifier.TryParse(recipeid, out recipeIdentifier) == false)
            {
                text = string.Format("Recipe [{0}] does not exist.", recipeid);
                this.WriteLog(LogLevel.Highest, text);
                ret = ErrorManager.Register(text);
                return ret;
            }

            recipe = RecipeManager.LoadRecipe(recipeIdentifier);
            if (recipe == null)
            {
                text = string.Format("Recipe [{0}] does not exist.", recipeid);
                this.WriteLog(LogLevel.Highest, text);
                ret = ErrorManager.Register(text);
                return ret;
            }

            if (recipe.Verified == false)
            {
                text = string.Format("Recipe [{0}] is not verified.", recipeid);
                this.WriteLog(LogLevel.Highest, text);
                ret = ErrorManager.Register(text);
                return ret;
            }
            #endregion

            processJobObjIds = new StringCollection();
            for (int i = 0; i < carrierids.Length; i++)
            {
                if ((ret = ProcessJobAttribute.Create(jobid + "-" + carrierids[i], carrierids[i], recipeid, out processJobAttribute)) != 0) return ret;

                // process job
                if ((ret = processJobAttribute.CreateInstance(out processJob)) != 0)
                {
                    Error error = ErrorManager.GetByUid(ret);
                    if (error != null)
                        Log.Write(this, new LogEntry(LogLevel.Highest, string.Format("processJobAttribute.CreateInstance() failed : {0}", error.ToString())));
                    else
                        Log.Write(this, new LogEntry(LogLevel.Highest, string.Format("processJobAttribute.CreateInstance() failed : {0}", "Unknown")));

                    return ret;
                }
                // create
                if ((ret = ProcessJobManager.Create(processJob)) != 0)
                {
                    Error error = ErrorManager.GetByUid(ret);
                    if (error != null)
                        Log.Write(this, new LogEntry(LogLevel.Highest, string.Format("ProcessJobManager.Create() failed : {0}", error.ToString())));
                    else
                        Log.Write(this, new LogEntry(LogLevel.Highest, string.Format("ProcessJobManager.Create() failed : {0}", "Unknown")));
                    return ret;
                }
                processJobObjIds.Add(processJob.ObjId);
            }

            // control job
            if ((ret = ControlJobAttribute.Create(jobid, carrierids, processJobObjIds, out controlJobAttribute)) != 0) return ret;
            if ((ret = controlJobAttribute.CreateInstance(out controlJob)) != 0)
            {
                Error error = ErrorManager.GetByUid(ret);
                if (error != null)
                    Log.Write(this, new LogEntry(LogLevel.Highest, string.Format("controlJobAttribute.CreateInstance() failed : {0}", error.ToString())));
                else
                    Log.Write(this, new LogEntry(LogLevel.Highest, string.Format("controlJobAttribute.CreateInstance() failed : {0}", "Unknown")));
                return ret;
            }
            if ((ret = ControlJobManager.Create(controlJob)) != 0)
            {
                Error error = ErrorManager.GetByUid(ret);
                if (error != null)
                    Log.Write(this, new LogEntry(LogLevel.Highest, string.Format("ControlJobManager.Create() failed : {0}", error.ToString())));
                else
                    Log.Write(this, new LogEntry(LogLevel.Highest, string.Format("ControlJobManager.Create() failed : {0}", "Unknown")));
                return ret;
            }

            return ret;
        }

        public int StartControlJob(string jobid)
        {
            int ret = 0;

            if ((ret = ControlJobManager.Start(jobid)) != 0)
            {
                Error error = ErrorManager.GetByUid(ret);
                if (error != null)
                    Log.Write(this, new LogEntry(LogLevel.Highest, string.Format("ControlJobManager.Start() failed : {0}", error.ToString())));
                else
                    Log.Write(this, new LogEntry(LogLevel.Highest, string.Format("ControlJobManager.Start() failed : {0}", "Unknown")));
                return ret;
            }

            return ret;
        }

        public virtual int PauseControlJob(string jobid)
        {
            int ret = 0;

            if ((ret = ControlJobManager.Pause(jobid)) != 0)
            {
                Error error = ErrorManager.GetByUid(ret);
                if (error != null)
                    Log.Write(this, new LogEntry(LogLevel.Highest, string.Format("ControlJobManager.Pause() failed : {0}", error.ToString())));
                else
                    Log.Write(this, new LogEntry(LogLevel.Highest, string.Format("ControlJobManager.Pause() failed : {0}", "Unknown")));
                return ret;
            }
            return ret;
        }

        public virtual int ResumeControlJob(string jobid)
        {
            int ret = 0;

            if ((ret = ControlJobManager.Resume(jobid)) != 0)
            {
                Error error = ErrorManager.GetByUid(ret);
                if (error != null)
                    Log.Write(this, new LogEntry(LogLevel.Highest, string.Format("ControlJobManager.Resume() failed : {0}", error.ToString())));
                else
                    Log.Write(this, new LogEntry(LogLevel.Highest, string.Format("ControlJobManager.Resume() failed : {0}", "Unknown")));
                return ret;
            }
            return ret;
        }

        public virtual int StopControlJob(string jobid)
        {
            int ret = 0;

            if ((ret = ControlJobManager.Stop(jobid, null)) != 0)
            {
                Error error = ErrorManager.GetByUid(ret);
                if (error != null)
                    Log.Write(this, new LogEntry(LogLevel.Highest, string.Format("ControlJobManager.Stop() failed : {0}", error.ToString())));
                else
                    Log.Write(this, new LogEntry(LogLevel.Highest, string.Format("ControlJobManager.Stop() failed : {0}", "Unknown")));
                return ret;
            }

            return ret;
        }
        #endregion

        #region SecsService Members
        protected override void SetBrokers()
        {
            SecsMessageBrokerConfiguration configuration = null;

            base.SetBrokers();

            // Remote Command Send (RCS)
            configuration = new SecsMessageBrokerConfiguration(2, 21, typeof(S2F21Broker));
            this.Brokers.Add(configuration);
            // Host Command Send (HCS)
            configuration = new SecsMessageBrokerConfiguration(2, 41, typeof(S2F41Broker));
            this.Brokers.Add(configuration);

            // Recipe List Request
            //configuration = new SecsMessageBrokerConfiguration(15, 201, typeof(S15F201Broker));
            //this.Brokers.Add(configuration);
        }
        #endregion

        #region Element
        protected new GeneralGemServiceConstructConfiguration ConstructConfiguration
        {
            get { return base.ConstructConfiguration as GeneralGemServiceConstructConfiguration; }
        }

        protected override ElementConstructConfiguration OnGetDefaultConstructConfiguration()
        {
            return new GeneralGemServiceConstructConfiguration();
        }

        protected override void OnSetConstructConfiguration(ElementConstructConfiguration configuration)
        {
            base.OnSetConstructConfiguration(configuration);

            if (this.ConstructConfiguration == null) return;
        }

        protected override void OnInstantiated()
        {
            base.OnInstantiated();

            this.SetRemoteCommand();
        }
        #endregion
    }
    #endregion

    #region GeneralGemServiceConstructConfiguration
    [Serializable]
    public class GeneralGemServiceConstructConfiguration : GemServiceConstructConfiguration
    {
        #region Field
        #endregion

        #region Constructor
        public GeneralGemServiceConstructConfiguration(ElementConstructMethod constructMethod)
            : base(constructMethod)
        {
        }
        public GeneralGemServiceConstructConfiguration() : this(ElementConstructMethod.Static) { }
        #endregion

        #region Property
        #endregion

        #region ConstructConfiguration
        protected override void SetDefaultValues()
        {
            base.SetDefaultValues();
        }
        #endregion
    }
    #endregion
}