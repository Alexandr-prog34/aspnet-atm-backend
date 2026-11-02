#!/bin/sh
set -eu
KCADM=/opt/keycloak/bin/kcadm.sh
until "$KCADM" config credentials --server "$KEYCLOAK_INTERNAL_URL" --realm master \
  --user "$KEYCLOAK_ADMIN_USER" --password "$KEYCLOAK_ADMIN_PASSWORD" >/dev/null 2>&1; do
  sleep 3
done
client_id=$("$KCADM" get clients -r "$KEYCLOAK_REALM" -q "clientId=$KEYCLOAK_CLIENT_ID" --fields id --format csv | tail -n 1 | tr -d '"')
scope_row=$("$KCADM" get client-scopes -r "$KEYCLOAK_REALM" --fields id,name --format csv | grep ',"basic"')
basic_scope_id=$(printf '%s' "${scope_row%%,*}" | tr -d '"')
"$KCADM" update "clients/$client_id/default-client-scopes/$basic_scope_id" -r "$KEYCLOAK_REALM" >/dev/null
create_user() {
  username="$1"
  password="$2"
  role="$3"
  if ! "$KCADM" get users -r "$KEYCLOAK_REALM" -q "username=$username" --fields username --format csv 2>/dev/null | grep -q "$username"; then
    "$KCADM" create users -r "$KEYCLOAK_REALM" -s "username=$username" -s enabled=true \
      -s firstName=Alexandr -s lastName=Plotnikov \
      -s "email=$username@local.test" -s emailVerified=true >/dev/null
  fi
  user_id=$("$KCADM" get users -r "$KEYCLOAK_REALM" -q "username=$username" --fields id --format csv | tail -n 1 | tr -d '"')
  "$KCADM" update "users/$user_id" -r "$KEYCLOAK_REALM" -s firstName=Alexandr -s lastName=Plotnikov \
    -s "email=$username@local.test" -s emailVerified=true -s 'requiredActions=[]' >/dev/null
  "$KCADM" set-password -r "$KEYCLOAK_REALM" --username "$username" --new-password "$password" >/dev/null
  "$KCADM" add-roles -r "$KEYCLOAK_REALM" --uusername "$username" --rolename "$role" >/dev/null
}
create_user "$ATM_TEST_USER_NAME" "$ATM_TEST_USER_PASSWORD" user
create_user "$ATM_TEST_ADMIN_NAME" "$ATM_TEST_ADMIN_PASSWORD" admin
