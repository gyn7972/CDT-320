using System;
using System.Threading.Tasks;
using QMC.Common.IO;

namespace QMC.CDT_320.Ui.Controls
{
    public enum IoCylinderItemType
    {
        Input,
        Output,
        Cylinder
    }

    public sealed class IoCylinderItem
    {
        public string DisplayName { get; set; }
        public IoCylinderItemType ItemType { get; set; }
        public Func<bool> StateGetter { get; set; }
        public Func<bool, Task<int>> OutputWriter { get; set; }
        public Func<Task<int>> ForwardCommand { get; set; }
        public Func<Task<int>> BackwardCommand { get; set; }
        public Func<Task<int>> OffCommand { get; set; }
        public bool CanControl { get; set; }
        public string OnText { get; set; }
        public string OffText { get; set; }

        public IoCylinderItem()
        {
            try
            {
                DisplayName = string.Empty;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public static IoCylinderItem Input(string displayName, Func<bool> stateGetter)
        {
            try
            {
                return new IoCylinderItem
                {
                    DisplayName = displayName,
                    ItemType = IoCylinderItemType.Input,
                    StateGetter = stateGetter,
                    CanControl = false
                };
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public static IoCylinderItem Output(string displayName, Func<bool> stateGetter, Action<bool> writer)
        {
            try
            {
                return Output(displayName, stateGetter, value =>
                {
                    try
                    {
                        writer?.Invoke(value);
                        return Task.FromResult(0);
                    }
                    catch
                    {
                        throw;
                    }
                    finally
                    {
                    }
                });
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public static IoCylinderItem Output(string displayName, Func<bool> stateGetter, Func<bool, Task<int>> writer)
        {
            try
            {
                return new IoCylinderItem
                {
                    DisplayName = displayName,
                    ItemType = IoCylinderItemType.Output,
                    StateGetter = stateGetter,
                    OutputWriter = writer,
                    CanControl = writer != null
                };
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public static IoCylinderItem Output(string displayName, Func<bool> stateGetter, Func<bool, Task<int>> writer, string onText, string offText)
        {
            try
            {
                var item = Output(displayName, stateGetter, writer);
                item.OnText = onText;
                item.OffText = offText;
                return item;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public static IoCylinderItem Cylinder(string displayName, BaseCylinder cylinder)
        {
            try
            {
                return new IoCylinderItem
                {
                    DisplayName = displayName,
                    ItemType = IoCylinderItemType.Cylinder,
                    StateGetter = () => cylinder != null && cylinder.IsFwd,
                    ForwardCommand = async () => await MoveCylinderAsync(cylinder, true),
                    BackwardCommand = async () => await MoveCylinderAsync(cylinder, false),
                    OffCommand = () =>
                    {
                        try
                        {
                            if (cylinder == null)
                                return Task.FromResult(-1);

                            cylinder.OutFwd.Off();
                            cylinder.OutBwd.Off();
                            return Task.FromResult(0);
                        }
                        catch
                        {
                            throw;
                        }
                        finally
                        {
                        }
                    },
                    CanControl = cylinder != null
                };
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        private static async Task<int> MoveCylinderAsync(BaseCylinder cylinder, bool forward)
        {
            try
            {
                if (cylinder == null)
                    return -1;

                bool result = forward ? await cylinder.MoveFwdAsync() : await cylinder.MoveBwdAsync();
                return result ? 0 : -1;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }
    }
}
