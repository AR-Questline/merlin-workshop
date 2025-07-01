using System;
using System.Collections.Generic;
using Unity.Services.UserReporting.Client;
using UnityEngine;

namespace Unity.Services.UserReporting
{
    static class LogDispatcher
    {
        static LogDispatcher()
        {
            s_Listeners = new List<WeakReference>();
            Application.logMessageReceivedThreaded += (logString, stackTrace, logType) =>
            {
                lock (s_Listeners)
                {
                    int i = 0;
                    while (i < s_Listeners.Count)
                    {
                        WeakReference listener = s_Listeners[i];
                        if (listener.Target is UserReportingPlatform logListener)
                        {
                            logListener.ReceiveLogMessage(logString, stackTrace, logType);
                            i++;
                        }
                        else
                        {
                            s_Listeners.RemoveAt(i);
                        }
                    }
                }
            };
        }

        static List<WeakReference> s_Listeners;

        public static void Register(UserReportingPlatform logListener)
        {
            lock (s_Listeners)
            {
                s_Listeners.Add(new WeakReference(logListener));
            }
        }
    }
}
