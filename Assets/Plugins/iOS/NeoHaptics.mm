#import <UIKit/UIKit.h>

extern "C" {
    // style: 0 = hafif, 1 = orta, 2 = guclu
    void _NeoHaptic(int style) {
        if (@available(iOS 10.0, *)) {
            UIImpactFeedbackStyle feedbackStyle = UIImpactFeedbackStyleLight;
            if (style == 1) feedbackStyle = UIImpactFeedbackStyleMedium;
            else if (style >= 2) feedbackStyle = UIImpactFeedbackStyleHeavy;

            dispatch_async(dispatch_get_main_queue(), ^{
                UIImpactFeedbackGenerator *generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:feedbackStyle];
                [generator prepare];
                [generator impactOccurred];
            });
        }
    }
}
