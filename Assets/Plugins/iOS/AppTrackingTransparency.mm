#import <AppTrackingTransparency/AppTrackingTransparency.h>
#import <AdSupport/AdSupport.h>

extern "C" {
    typedef void (*ATTCallback)(int status);

    void _RequestTrackingAuthorization(ATTCallback callback) {
        if (callback == NULL) {
            return;
        }

        NSString *usage = [[NSBundle mainBundle] objectForInfoDictionaryKey:@"NSUserTrackingUsageDescription"];
        if (usage == nil || [usage length] == 0) {
            callback(2);
            return;
        }

        if (@available(iOS 14, *)) {
            [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status) {
                callback((int)status);
            }];
        } else {
            // Fallback for older iOS versions
            callback(3); // Authorized
        }
    }
}
