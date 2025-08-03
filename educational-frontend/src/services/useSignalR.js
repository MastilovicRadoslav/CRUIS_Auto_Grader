import { useEffect } from "react";
import * as signalR from "@microsoft/signalr";

export const useSignalR = (onStatusChanged) => {
  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl("http://localhost:8285/statusHub") // URL ka backend hub-u
      .withAutomaticReconnect()
      .build();

    connection.on("StatusChanged", (data) => {
      console.log("📡 Status changed:", data);
      if (onStatusChanged) {
        onStatusChanged(data);
      }
    });

    connection.start()
      .then(() => {
        console.log("✅ Connected to SignalR hub");
      })
      .catch((err) => {
        console.error("SignalR connection error:", err);
      });

    return () => {
      connection.stop();
    };
  }, [onStatusChanged]);
};
