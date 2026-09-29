// Native haptics bridge for MoonPull.Haptics.NativeHapticsService (DllImport "__Internal").
#import <UIKit/UIKit.h>

extern "C" void MoonPull_Haptic(int type)
{
    dispatch_async(dispatch_get_main_queue(), ^{
        switch (type)
        {
            case 0: // Selection
            {
                UISelectionFeedbackGenerator *generator = [[UISelectionFeedbackGenerator alloc] init];
                [generator selectionChanged];
                break;
            }
            case 1: // Light
            {
                UIImpactFeedbackGenerator *generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
                [generator impactOccurred];
                break;
            }
            case 2: // Medium
            {
                UIImpactFeedbackGenerator *generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
                [generator impactOccurred];
                break;
            }
            case 3: // Heavy
            {
                UIImpactFeedbackGenerator *generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleHeavy];
                [generator impactOccurred];
                break;
            }
            default: // Success
            {
                UINotificationFeedbackGenerator *generator = [[UINotificationFeedbackGenerator alloc] init];
                [generator notificationOccurred:UINotificationFeedbackTypeSuccess];
                break;
            }
        }
    });
}
