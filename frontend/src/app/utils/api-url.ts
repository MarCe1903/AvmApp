import { environment } from '../../environments/environment';

function trimTrailingSlash(value: string): string {
  return value.replace(/\/+$/, '');
}

function trimLeadingSlash(value: string): string {
  return value.replace(/^\/+/, '');
}

export function buildApiUrl(path: string): string {
  return `${trimTrailingSlash(environment.apiUrl)}/${trimLeadingSlash(path)}`;
}
