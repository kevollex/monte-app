import { createContext } from '@lit/context';
import type { AuthService } from './auth-service';
export type { AuthService } from './auth-service';

export const authServiceContext = createContext<AuthService>('auth-service');