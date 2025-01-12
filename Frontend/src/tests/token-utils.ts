export function createValidAccessToken(payload?: AdditionalPayload): string {
    const now = Math.floor(Date.now() / 1000);
    const payloadWithExp: JwtPayload = {
        exp: now + 3600,
        id: "123",
        unique_name: "test",
        given_name: "test",
        email: "test@test.com",
        ...payload
    };
    return createToken(payloadWithExp);
}

export function createExpiredAccessToken(payload?: AdditionalPayload): string {
    const now = Math.floor(Date.now() / 1000);
    const payloadWithExp: JwtPayload = {
        exp: now - 3600,
        id: "123",
        unique_name: "test",
        given_name: "test",
        email: "test@test.com",
        ...payload
    };
    return createToken(payloadWithExp);
}

export function createToken(payload: JwtPayload): string {
    const header = {
        "alg": "HS256",
        "typ": "JWT"
    };
    const headerBase64 = btoa(JSON.stringify(header));
    const payloadBase64 = btoa(JSON.stringify(payload));
    // Fake signature, not needed for any tests
    const signature = btoa("signature");
    return `${headerBase64}.${payloadBase64}.${signature}`;
}

export type AdditionalPayload = {
    [key: string]: any;
}

export type JwtPayload = {
    exp: number;
    id: string;
    unique_name: string;
    given_name: string;
    email: string;
    [key: string]: any;
}
