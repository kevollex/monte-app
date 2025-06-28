import { httpClient } from '../http-client';

export class AuthService {

    async login(email : string, password: string): Promise<string> {
        try {
            const response = await httpClient.post<string>(
                `/api/login`,
                { email, password },
            );
            return response.data;
        } catch (error) {
            console.error('Error login:', error);
            throw error; // Re-throw the error for further handling if needed
        }
    }
}