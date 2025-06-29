import { createContext } from '@lit/context';
import type { MontessoriBoWrapperService } from './montessoribowrapper-service';
export type { MontessoriBoWrapperService } from './montessoribowrapper-service';

export const montessoriBoWrapperServiceContext = createContext<MontessoriBoWrapperService>('montessoribowrapper-service');