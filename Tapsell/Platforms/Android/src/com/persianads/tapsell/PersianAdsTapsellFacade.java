package com.persianads.tapsell;

import android.app.Application;
import android.content.Context;

import java.lang.reflect.Method;

import ir.tapsell.sdk.Tapsell;
import ir.tapsell.sdk.TapsellAdRequestListener;
import ir.tapsell.sdk.TapsellAdRequestOptions;
import ir.tapsell.sdk.TapsellAdShowListener;
import ir.tapsell.sdk.TapsellShowOptions;

public final class PersianAdsTapsellFacade {
    private PersianAdsTapsellFacade() {
    }

    public static void initialize(Application application, String appId, boolean debugMode) {
        Tapsell.initialize(application, appId);
        Tapsell.setDebugMode(application, debugMode);
    }

    public static void requestAd(Context context, String zoneId, Object callback) {
        Tapsell.requestAd(context, zoneId, new TapsellAdRequestOptions(), new TapsellAdRequestListener() {
            @Override
            public void onAdAvailable(String adId) {
                invoke(callback, "onAdAvailable", new Class<?>[]{String.class}, new Object[]{adId});
            }

            @Override
            public void onError(String message) {
                invoke(callback, "onError", new Class<?>[]{String.class}, new Object[]{message});
            }

            @Override
            public void onNoAdAvailable() {
                invoke(callback, "onNoAdAvailable", new Class<?>[]{}, new Object[]{});
            }

            @Override
            public void onNoNetwork() {
                invoke(callback, "onNoNetwork", new Class<?>[]{}, new Object[]{});
            }
        });
    }

    public static void showAd(Context context, String zoneId, String adId, boolean backDisabled, boolean immersiveMode,
                              int rotationMode, boolean showDialog, Object callback) {
        TapsellShowOptions options = new TapsellShowOptions();
        options.setBackDisabled(backDisabled);
        options.setImmersiveMode(immersiveMode);
        options.setRotationMode(rotationMode);
        options.setShowDialog(showDialog);

        Tapsell.showAd(context, zoneId, adId, options, new TapsellAdShowListener() {
            @Override
            public void onOpened() {
                invoke(callback, "onOpened", new Class<?>[]{}, new Object[]{});
            }

            @Override
            public void onClosed() {
                invoke(callback, "onClosed", new Class<?>[]{}, new Object[]{});
            }

            @Override
            public void onError(String message) {
                invoke(callback, "onError", new Class<?>[]{String.class}, new Object[]{message});
            }

            @Override
            public void onRewarded(boolean completed) {
                invoke(callback, "onRewarded", new Class<?>[]{boolean.class}, new Object[]{completed});
            }

            @Override
            public void onAdClicked() {
                invoke(callback, "onAdClicked", new Class<?>[]{}, new Object[]{});
            }
        });
    }

    public static String getVastTag(String zoneId) {
        return Tapsell.getVastTag(zoneId);
    }

    private static void invoke(Object target, String methodName, Class<?>[] parameterTypes, Object[] args) {
        if (target == null) {
            return;
        }

        try {
            Method method = target.getClass().getMethod(methodName, parameterTypes);
            method.invoke(target, args);
        } catch (Throwable ignored) {
        }
    }
}
