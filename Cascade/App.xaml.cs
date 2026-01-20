using System.Windows;
using Cascade.Models.Operations;
using Cascade.Services;
using Cascade.Views.Operations.FFmpeg;
using Cascade.ViewModels.Operations.FFmpeg;
using Cascade.Views.Operations.CascadeIO;
using Cascade.ViewModels.Operations.CascadeIO;

namespace Cascade
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // 初始化本地化服务，加载保存的语言设置
            LocalizationService.Initialize();
            
            RegisterOperationSubPages();
        }

        /// <summary>
        /// 注册所有操作子页面
        /// </summary>
        private void RegisterOperationSubPages()
        {
            var registry = OperationRegistry.Instance;

            // Cascade 输入与输出 子页面
            registry.RegisterSubPage("cascade-io", new OperationSubPageInfo
            {
                Id = "cascade-io-naming-output",
                DisplayNameKey = "Operations_SubPage_NamingOutput",
                ViewType = typeof(NamingAndOutputView),
                ViewModelType = typeof(NamingAndOutputViewModel),
                Order = 10
            });

            // FFmpeg 子页面
            registry.RegisterSubPage("ffmpeg", new OperationSubPageInfo
            {
                Id = "ffmpeg-size-layout",
                DisplayNameKey = "Operations_SubPage_SizeLayout",
                ViewType = typeof(SizeAndLayoutView),
                ViewModelType = typeof(SizeAndLayoutViewModel),
                Order = 10
            });

            registry.RegisterSubPage("ffmpeg", new OperationSubPageInfo
            {
                Id = "ffmpeg-video-encoder-strategy",
                DisplayNameKey = "Operations_SubPage_VideoEncoderStrategy",
                ViewType = typeof(VideoEncoderStrategyView),
                ViewModelType = typeof(VideoEncoderStrategyViewModel),
                Order = 20
            });

            registry.RegisterSubPage("ffmpeg", new OperationSubPageInfo
            {
                Id = "ffmpeg-muxing-delivery",
                DisplayNameKey = "Operations_SubPage_MuxingDelivery",
                ViewType = typeof(MuxingAndDeliveryView),
                ViewModelType = typeof(MuxingAndDeliveryViewModel),
                Order = 30
            });
        }
    }
}
