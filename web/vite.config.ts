import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  resolve: {
    preserveSymlinks: true,
  },
  server: {
    port: 5173,
    proxy: {
      "/api": { target: "http://localhost:5031", changeOrigin: true },
      "/hubs": { target: "http://localhost:5031", changeOrigin: true, ws: true },
    },
  },
  preview: {
    port: 4173,
    proxy: {
      "/api": { target: "http://localhost:5031", changeOrigin: true },
      "/hubs": { target: "http://localhost:5031", changeOrigin: true, ws: true },
    },
  },
});
