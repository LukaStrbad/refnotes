FROM node:22-slim AS build
ENV PNPM_HOME="/pnpm"
ENV PATH="$PNPM_HOME:$PATH"
RUN corepack enable

WORKDIR /app

# Install dependencies
COPY package.json pnpm-lock.yaml /app/
RUN pnpm install

# Copy the rest
COPY . /app/
RUN pnpm run build

FROM caddy:alpine
COPY --from=build /app/dist/frontend/browser /srv
COPY docker/Caddyfile /etc/caddy/Caddyfile

CMD ["caddy", "run", "--config", "/etc/caddy/Caddyfile"]
