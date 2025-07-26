FROM node:22-slim AS build
ENV PNPM_HOME="/pnpm"
ENV PATH="$PNPM_HOME:$PATH"
RUN corepack enable

ARG WEB_DOMAIN
ARG WEB_URL

WORKDIR /app

# Install dependencies
COPY ./src/Web/package.json ./src/Web/pnpm-lock.yaml /app/
RUN pnpm install

# Copy the rest
COPY ./src/Web /app/

# Exchange environment variables from environment.prod.ts
RUN sed -i "s|{{WEB_DOMAIN}}|${WEB_DOMAIN}|g" /app/src/environments/environment.prod.ts
RUN sed -i "s|{{WEB_URL}}|${WEB_URL}|g" /app/src/environments/environment.prod.ts

RUN pnpm run build

FROM caddy:alpine
COPY --from=build /app/dist/web/browser /srv
COPY docker/web/Caddyfile /etc/caddy/Caddyfile

CMD ["caddy", "run", "--config", "/etc/caddy/Caddyfile"]
