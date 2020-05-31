using System.Threading.Tasks;

namespace KitZ.Extensions
{
    public static class TaskExtensions
    {
        public static void Forget(this Task task)
        {
            if (!task.IsCompleted || task.IsFaulted)
                _ = ForgetAwaited();

            async Task ForgetAwaited()
            {
                try
                {
                    await task.ConfigureAwait(false);
                }
                catch
                {
                    // Nothing to do here
                }
            }
        }
    }
}