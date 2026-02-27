#import <AppTrackingTransparency/AppTrackingTransparency.h>
#import <AdSupport/AdSupport.h>

extern "C" {
    typedef void (*ATTCallback)(int status);

    void _RequestTrackingAuthorization(ATTCallback callback) {
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
