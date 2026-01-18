// Authentication helper functions for Blazor interop
window.performLogin = async function(credentials) {
    try {
        const response = await fetch('/api/ui-auth/login', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                username: credentials.username,
                password: credentials.password,
                profileName: credentials.profileName,
                apiGatewayUrl: credentials.apiGatewayUrl
            }),
            credentials: 'include' // Important: include cookies in the request/response
        });

        if (response.ok) {
            return { success: true };
        } else {
            const errorData = await response.json().catch(() => null);
            return {
                success: false,
                error: errorData?.error || 'Invalid username or password'
            };
        }
    } catch (error) {
        return {
            success: false,
            error: error.message || 'Login failed'
        };
    }
};
