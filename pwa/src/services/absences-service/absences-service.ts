import { httpClient } from '../http-client';

export class AbsencesService {

    // TODO: Remove example when we are done testing.
    async getSqlServerInfo(): Promise<string> {
        try {
            const response = await httpClient.get<string>("api/sqlserverinfo");
            return response.data;
        } catch (error) {
            console.error('Error fetching SQL Server info:', error);
            throw error; // Re-throw the error for further handling if needed
        }
    }
    // TODO: Remove example when we are done testing.
    async getLicenciasPoC(email: string, password: string): Promise<string> {
        if (!email || !password) {
            throw new Error('Email and password are required.');
        }
        try {
            const response = await httpClient.get<string>(
                `/api/licenciaspoc?email=${encodeURIComponent(email)}&password=${encodeURIComponent(password)}`
            );
            return response.data;
        } catch (error) {
            console.error('Error fetching Licencias PoC:', error);
            throw error; // Re-throw the error for further handling if needed
        }
    }
}