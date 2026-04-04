export interface BuildVersionInfo {
  name: string;
  version: string;
  commit: string;
  buildDate: string;
  environment: string;
}

export interface VersionDisplayInfo {
  frontend: BuildVersionInfo;
  backend: BuildVersionInfo | null;
}
