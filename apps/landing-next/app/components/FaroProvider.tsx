"use client";

import { ReactNode, useEffect } from "react";
import { initializeFaro, getWebInstrumentations, faro, LogLevel } from "@grafana/faro-web-sdk";
import { TracingInstrumentation } from "@grafana/faro-web-tracing";

let initialized = false;

export function FaroProvider({ children, env }: { children: ReactNode; env: string }) {
  useEffect(() => {
    if (initialized) return;

    const url = process.env.NEXT_PUBLIC_FARO_URL;
    if (!url) return;

    const appName = process.env.NEXT_PUBLIC_FARO_APP_NAME ?? "landing";

    initializeFaro({
      url,
      app: {
        name: appName,
        environment: env,
      },
      instrumentations: [
        ...getWebInstrumentations({
          captureConsole: true,
          captureConsoleDisabledLevels: [],
        }),
        new TracingInstrumentation(),
      ],
    });

    initialized = true;
    faro.api.pushLog(["Faro initialized"], { level: LogLevel.INFO });
  }, [env]);

  return <>{children}</>;
}
