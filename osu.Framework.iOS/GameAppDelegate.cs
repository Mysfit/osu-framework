﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using UIKit;
using Foundation;
using System.Drawing;
using SixLabors.ImageSharp.PixelFormats;
using AVFoundation;
using System;

namespace osu.Framework.iOS
{
    public abstract class GameAppDelegate : UIApplicationDelegate
    {
        public override UIWindow Window { get; set; }

        private IOSGameView gameView;
        private IOSGameHost host;

        protected abstract Game CreateGame();

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            aotImageSharp();

            Window = new UIWindow(UIScreen.MainScreen.Bounds);

            gameView = new IOSGameView(new RectangleF(0.0f, 0.0f, (float)Window.Frame.Size.Width, (float)Window.Frame.Size.Height));
            host = new IOSGameHost(gameView);

            Window.RootViewController = new GameViewController(gameView, host);
            Window.MakeKeyAndVisible();

            // required to trigger the osuTK update loop, which is used for input handling.
            gameView.Run();

            host.Run(CreateGame());

            // Watch for the volume button changing in order to change audio policy
            AVAudioSession audioSession = AVAudioSession.SharedInstance();
            audioSession.AddObserver(this, "outputVolume", NSKeyValueObservingOptions.New, IntPtr.Zero);

            return true;
        }

        private void aotImageSharp()
        {
            System.Runtime.CompilerServices.Unsafe.SizeOf<Rgba32>();
            System.Runtime.CompilerServices.Unsafe.SizeOf<long>();

            try
            {
                new SixLabors.ImageSharp.Formats.Png.PngDecoder().Decode<Rgba32>(SixLabors.ImageSharp.Configuration.Default, null);
            }
            catch
            {
            }
        }

        public override void ObserveValue(NSString keyPath, NSObject ofObject, NSDictionary change, IntPtr context)
        {
            switch (keyPath)
            {
                case "outputVolume":
                    AVAudioSession.SharedInstance().SetCategory(AVAudioSessionCategory.Playback);
                    break;
            }
        }

        public override void DidEnterBackground(UIApplication application) => host.Suspend();

        public override void WillEnterForeground(UIApplication application) => host.Resume();
    }
}
