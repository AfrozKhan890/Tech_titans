#!/usr/bin/env bash
# End-to-end curl verification harness for the MarketLink MVC app.
# Usage:  BASE_URL=http://localhost:5230 bash scripts/verify-mvc.sh   (default http://localhost:5229)
# Requires the app to be running. It signs in as each seeded role and leaves behind one
# completed order plus its notifications, so re-seed afterwards if you want a pristine DB.
cd "$(dirname "$0")" || exit 1
BASE="${BASE_URL:-http://localhost:5229}"

TMP="$(mktemp -d)"
JAR_ADMIN="$TMP/admin.jar"; JAR_FARMER="$TMP/farmer.jar"; JAR_CUST="$TMP/cust.jar"; JAR_ANON="$TMP/anon.jar"

PASS=0; FAIL=0
section() { printf '\n== %s ==\n' "$1"; }
ok()   { PASS=$((PASS+1)); echo "  OK   $1"; }
bad()  { FAIL=$((FAIL+1)); echo "  FAIL $1"; }

dump_body() { # last 15 meaningful lines of a response body, tags stripped
  [ -f "$1" ] || return 0
  sed -e 's/<[^>]*>//g' -e 's/&[a-z]*;//g' "$1" | grep -v '^[[:space:]]*$' | tail -15 | sed 's/^/       | /'
}

# G <jar|''> <path> <outfile> -> prints http code; headers go to <outfile>.h
G() {
  local jar="$1" path="$2" out="$3"
  if [ -n "$jar" ]; then
    curl -s -m 20 -b "$jar" -c "$jar" -o "$out" -D "$out.h" -w '%{http_code}' "$BASE$path"
  else
    curl -s -m 20 -o "$out" -D "$out.h" -w '%{http_code}' "$BASE$path"
  fi
}
# P <jar> <path> <outfile> <extra curl args...> -> prints http code
P() {
  local jar="$1" path="$2" out="$3"; shift 3
  curl -s -m 20 -b "$jar" -c "$jar" -o "$out" -D "$out.h" -w '%{http_code}' "$@" "$BASE$path"
}
chk() { # <desc> <expected> <actual> <bodyfile>
  if [ "$2" = "$3" ]; then ok "$1 [HTTP $3]"; else bad "$1 expected HTTP $2, got $3 ($BASE$4)"; dump_body "$5"; fi
}
contains() { # <desc> <file> <needle>
  if grep -qF -- "$3" "$2" 2>/dev/null; then ok "$1"; else bad "$1: content '$3' missing"; dump_body "$2"; fi
}
not_contains() {
  if grep -qF -- "$3" "$2" 2>/dev/null; then bad "$1: unexpected content '$3'"; dump_body "$2"; else ok "$1"; fi
}
loc() { grep -i '^location:' "$1" | head -1 | tr -d '\r' | sed 's/^[Ll]ocation: //'; }
# scrape antiforgery token from an html file
tok() { tr -d '\n\r' < "$1" | grep -oP '__RequestVerificationToken[^>]*value="\K[^"]+' | head -1; }
# login <jar> <email> <password> -> prints code of login POST; logs body file in $TMP/login_last.html
login() {
  local jar="$1" email="$2" pass="$3" out="$TMP/login_last.html"
  # GET the form WITH the target jar so the antiforgery cookie lands there and matches the token
  curl -s -m 20 -c "$jar" -o "$out" -D "$out.h" "$BASE/Auth/Login"
  local t; t="$(tok "$out")"
  P "$jar" "/Auth/Login" "$out" --data-urlencode "Email=$email" --data-urlencode "Password=$pass" \
      --data-urlencode "__RequestVerificationToken=$t" -H "Content-Type: application/x-www-form-urlencoded"
}
# loud fail when a derived id/value is empty
need() { # <varname> <value> <context>
  if [ -z "$2" ]; then bad "derivable value $1 is EMPTY ($3) - aborting dependent checks"; return 1; fi
  return 0
}

# ============================================================================
section "1. PUBLIC PAGES"
# ============================================================================
for p in / /Products /Farmers /Markets /Auth/Login /Auth/Register; do
  code=$(G "" "$p" "$TMP/pub.html"); chk "GET $p" 200 "$code" "$p" "$TMP/pub.html"
done

G "" "/Markets" "$TMP/markets.html" > /dev/null
MARKET_IDS=$(grep -oE '/Markets/Details/[0-9]+' "$TMP/markets.html" | grep -oE '[0-9]+$' | sort -un)
if [ -z "$MARKET_IDS" ]; then
  bad "derive market ids from /Markets HTML (empty) - /Markets/Details checks skipped"; dump_body "$TMP/markets.html"
else
  for id in $MARKET_IDS; do
    code=$(G "" "/Markets/Details/$id" "$TMP/md_$id.html"); chk "GET /Markets/Details/$id" 200 "$code" "/Markets/Details/$id" "$TMP/md_$id.html"
  done
fi
G "" "/Farmers" "$TMP/farmers.html" > /dev/null
FARMER_IDS=$(grep -oE '/Farmers/Details/[0-9]+' "$TMP/farmers.html" | grep -oE '[0-9]+$' | sort -un)
if [ -z "$FARMER_IDS" ]; then
  bad "derive farmer ids from /Farmers HTML (empty)"; dump_body "$TMP/farmers.html"
else
  for id in $FARMER_IDS; do
    code=$(G "" "/Farmers/Details/$id" "$TMP/fd_$id.html"); chk "GET /Farmers/Details/$id" 200 "$code" "/Farmers/Details/$id" "$TMP/fd_$id.html"
  done
fi
code=$(G "" "/Markets/Details/9999" "$TMP/md9999.html"); chk "GET /Markets/Details/9999 is 404" 404 "$code" "/Markets/Details/9999" "$TMP/md9999.html"

# seeded product name via the products autocomplete API, then content-check pages
G "" "/Products/Autocomplete?q=Tomato" "$TMP/ac.json" > /dev/null
SEED_PRODUCT=$(grep -oP '"name":"\K[^"]+' "$TMP/ac.json" | head -1)
if need SEED_PRODUCT "$SEED_PRODUCT" "autocomplete returned no seeded product"; then
  echo "       seeded product name: '$SEED_PRODUCT'"
  G "" "/" "$TMP/home.html" > /dev/null
  contains "home page contains seeded product '$SEED_PRODUCT'" "$TMP/home.html" "$SEED_PRODUCT"
  G "" "/Products" "$TMP/products.html" > /dev/null
  contains "/Products list contains seeded product '$SEED_PRODUCT'" "$TMP/products.html" "$SEED_PRODUCT"
fi

# ============================================================================
section "2. AUTH"
# ============================================================================
code=$(login "$JAR_ADMIN"  "admin@marketlink.com" "Admin@123456")
[ "$code" = 302 ] && ok "admin login POST /Auth/Login -> 302" || { bad "admin login expected 302, got $code"; dump_body "$TMP/login_last.html"; }
code=$(login "$JAR_FARMER" "farmer1@marketlink.com" "Farmer@123456")
[ "$code" = 302 ] && ok "farmer login POST /Auth/Login -> 302" || { bad "farmer login expected 302, got $code"; dump_body "$TMP/login_last.html"; }
code=$(login "$JAR_CUST"  "customer1@marketlink.com" "Customer@123456")
[ "$code" = 302 ] && ok "customer login POST /Auth/Login -> 302" || { bad "customer login expected 302, got $code"; dump_body "$TMP/login_last.html"; }

code=$(G "$JAR_ADMIN"  "/Admin/Dashboard"   "$TMP/admin_dash.html");   chk "GET /Admin/Dashboard (admin)"   200 "$code" "/Admin/Dashboard"   "$TMP/admin_dash.html"
code=$(G "$JAR_FARMER" "/Farmer/Dashboard"  "$TMP/farmer_dash.html");  chk "GET /Farmer/Dashboard (farmer)" 200 "$code" "/Farmer/Dashboard"  "$TMP/farmer_dash.html"
code=$(G "$JAR_CUST"   "/Customer/Dashboard" "$TMP/cust_dash.html");   chk "GET /Customer/Dashboard (customer)" 200 "$code" "/Customer/Dashboard" "$TMP/cust_dash.html"

code=$(G "$JAR_ANON" "/Customer/Dashboard" "$TMP/anon_dash.html")
if [ "$code" = 302 ] && grep -qi '^location:.*login' "$TMP/anon_dash.html.h"; then
  ok "unauthenticated /Customer/Dashboard -> 302 to login (Location: $(loc "$TMP/anon_dash.html.h"))"
else
  bad "unauthenticated /Customer/Dashboard expected 302 to /Auth/Login, got $code -> $(loc "$TMP/anon_dash.html.h")"
fi

# ============================================================================
section "3. LINK AUDIT (no 500s / no broken routes on rendered pages)"
# ============================================================================
audit_role() { # <name> <jar> <pages...>
  local name="$1" jar="$2"; shift 2
  local flat="$TMP/links_$name.txt"; : > "$flat"
  local p
  for p in "$@"; do
    local f="$TMP/page_$name$(echo "$p" | tr '/?' '__').html"
    G "$jar" "$p" "$f" > /dev/null
    tr -d '\n\r' < "$f" | sed 's/&amp;/\&/g' >> "$flat"
    echo >> "$flat"
  done
  local links n=0 badn=0 l code lreport=""
  links=$(grep -oE 'href="[^"]*"' "$flat" | sed 's/^href="//;s/"$//' | sed 's/#.*$//' \
          | grep -E '^/[^/]' | grep -vE '^//' \
          | grep -viE '\.(css|js|png|jpe?g|svg|ico|gif|woff2?|ttf|eot|map)([?&]|$)' \
          | sort -u | head -70)
  while IFS= read -r l; do
    [ -z "$l" ] && continue
    n=$((n+1))
    code=$(G "$jar" "$l" "$TMP/aud.html")
    case "$code" in
      2*|3*) : ;;
      5*) badn=$((badn+1)); bad "[$name] $BASE$l -> HTTP $code (server error)"; dump_body "$TMP/aud.html" ;;
      *)  badn=$((badn+1)); bad "[$name] $BASE$l -> HTTP $code (non-2xx/3xx) $(loc "$TMP/aud.html.h" | head -c 120)"; dump_body "$TMP/aud.html" ;;
    esac
  done <<< "$links"
  if [ "$n" -eq 0 ]; then bad "[$name] link audit extracted 0 links - regex/page failure"; else
    ok "[$name] link audit: $n unique links, $badn problem link(s)"
  fi
}
audit_role customer "$JAR_CUST"  /Customer/Dashboard /Customer/Cart /Customer/Orders
audit_role farmer   "$JAR_FARMER" /Farmer/Dashboard /Farmer/Orders
audit_role admin    "$JAR_ADMIN"  /Admin/Dashboard

# ============================================================================
section "4. FAVORITES"
# ============================================================================
# derive a product id + name pair from market 1 product cards (name h6 precedes addItem button)
G "" "/Markets/Details/1" "$TMP/mk1.html" > /dev/null
PAIR=$(tr -d '\n\r' < "$TMP/mk1.html" | grep -oP 'card-title fw-bold">\K[^<]+</h6>.{0,700}?ML\.Cart\.addItem\([0-9]+' | head -1)
PRODUCT_NAME=$(echo "$PAIR" | sed 's|</h6>.*||')
PRODUCT_ID=$(echo "$PAIR" | grep -oP 'ML\.Cart\.addItem\(\K[0-9]+')
FAV_TOK=""
if need PRODUCT_ID "$PRODUCT_ID" "market page product id" && need PRODUCT_NAME "$PRODUCT_NAME" "market page product name"; then
  echo "       derived product: id=$PRODUCT_ID name='$PRODUCT_NAME'"
  G "$JAR_CUST" "/Customer/Favorites" "$TMP/favs.html" > /dev/null
  FAV_TOK=$(tok "$TMP/favs.html")
  if [ -z "$FAV_TOK" ]; then bad "no antiforgery token found on /Customer/Favorites"; fi
  favtoggle() { # <outfile>
    P "$JAR_CUST" "/Customer/Favorites/Toggle" "$1" \
      --data-urlencode "type=product" --data-urlencode "id=$PRODUCT_ID" \
      --data-urlencode "__RequestVerificationToken=$FAV_TOK" -H "Content-Type: application/x-www-form-urlencoded"
  }
  # normalize state: if already favorited from a previous run, clear it first
  if grep -qF -- "$PRODUCT_NAME" "$TMP/favs.html"; then
    favtoggle "$TMP/t0.json" > /dev/null
    echo "       pre-run favorite state detected; cleared before asserting"
  fi
  code=$(favtoggle "$TMP/t1.json")
  chk "POST /Customer/Favorites/Toggle (add)" 200 "$code" "/Customer/Favorites/Toggle" "$TMP/t1.json"
  if grep -q '"success":true' "$TMP/t1.json" && grep -q '"isFavorite":true' "$TMP/t1.json"; then
    ok "toggle add JSON: success:true isFavorite:true"
  else bad "toggle add JSON wrong: $(head -c 200 "$TMP/t1.json")"; fi
  code=$(favtoggle "$TMP/t2.json")
  if grep -q '"isFavorite":false' "$TMP/t2.json"; then ok "toggle remove JSON: isFavorite:false"; else bad "toggle remove JSON wrong: $(head -c 200 "$TMP/t2.json")"; fi
  # bogus id must not 500
  code=$(P "$JAR_CUST" "/Customer/Favorites/Toggle" "$TMP/t3.json" \
          --data-urlencode "type=product" --data-urlencode "id=999999" \
          --data-urlencode "__RequestVerificationToken=$FAV_TOK" -H "Content-Type: application/x-www-form-urlencoded")
  if [ "$code" = 500 ] || [ "${code:0:1}" = 5 ]; then bad "toggle bogus id -> HTTP $code (server error)"; dump_body "$TMP/t3.json"
  elif [ "$code" = 404 ] || { [ "$code" = 200 ] && grep -q '"success":false' "$TMP/t3.json"; }; then
    ok "toggle bogus id -> clean failure (HTTP $code, body: $(head -c 120 "$TMP/t3.json"))"
  else bad "toggle bogus id -> unexpected HTTP $code body $(head -c 120 "$TMP/t3.json")"; fi
  # re-add, then confirm favorites page renders the product; finally un-favorite to leave state clean
  favtoggle "$TMP/t4.json" > /dev/null
  code=$(G "$JAR_CUST" "/Customer/Favorites" "$TMP/favs2.html")
  chk "GET /Customer/Favorites renders" 200 "$code" "/Customer/Favorites" "$TMP/favs2.html"
  contains "/Customer/Favorites contains '$PRODUCT_NAME'" "$TMP/favs2.html" "$PRODUCT_NAME"
  favtoggle "$TMP/t5.json" > /dev/null
fi

# ============================================================================
section "5. SELLER -> BUYER -> ADMIN FLOW"
# ============================================================================
if [ -n "$PRODUCT_ID" ]; then
  CART_TOK=$(tok "$TMP/cust_dash.html")
  [ -z "$CART_TOK" ] && G "$JAR_CUST" "/Customer/Dashboard" "$TMP/cust_dash.html" > /dev/null && CART_TOK=$(tok "$TMP/cust_dash.html")
  code=$(P "$JAR_CUST" "/Customer/Cart/AddItem" "$TMP/add.json" -H "Content-Type: application/json" \
          -H "RequestVerificationToken: $CART_TOK" -d "{\"productId\":$PRODUCT_ID,\"quantityKg\":5}")
  chk "POST /Customer/Cart/AddItem JSON (pid=$PRODUCT_ID)" 200 "$code" "/Customer/Cart/AddItem" "$TMP/add.json"
  grep -q '"success":true' "$TMP/add.json" && ok "AddItem JSON success:true" || bad "AddItem JSON: $(head -c 200 "$TMP/add.json")"
  code=$(G "$JAR_CUST" "/Customer/Cart" "$TMP/cart.html")
  chk "GET /Customer/Cart" 200 "$code" "/Customer/Cart" "$TMP/cart.html"
  contains "cart shows '$PRODUCT_NAME'" "$TMP/cart.html" "$PRODUCT_NAME"

  # derive pickup fields from rendered cart (never hardcode slot ids)
  FLAT=$(tr -d '\n\r' < "$TMP/cart.html" | sed 's/&amp;/\&/g')
  IDX_LIST=$(echo "$FLAT" | grep -oE 'Pickups\[[0-9]+\]\.FarmerId' | grep -oE '[0-9]+' | sort -un)
  PICKUP_TIME=$(date -d "+2 days" +%Y-%m-%dT10:00 2>/dev/null || date -v+2d +%Y-%m-%dT10:00)
  CHECKOUT_OK=1
  DATA_ARGS=()
  for i in $IDX_LIST; do
    fid=$(echo "$FLAT" | grep -oP "name=\"Pickups\[$i\]\.FarmerId\"[^>]*value=\"\K[0-9]+" | head -1)
    sid=$(echo "$FLAT" | grep -oP "name=\"Pickups\[$i\]\.PickupSlotId\"[^>]*>[^<]*<option value=\"\K[0-9]+" | head -1)
    if ! need "Pickups[$i].FarmerId" "$fid" "cart farmer id"; then CHECKOUT_OK=0; fi
    if ! need "Pickups[$i].PickupSlotId" "$sid" "cart slot option id (no slot offered -> checkout will loop back to cart)"; then CHECKOUT_OK=0; fi
    DATA_ARGS+=(--data-urlencode "Pickups[$i].FarmerId=$fid" --data-urlencode "Pickups[$i].PickupSlotId=$sid" --data-urlencode "Pickups[$i].PickupTime=$PICKUP_TIME")
  done
  [ -z "$IDX_LIST" ] && { bad "no Pickups[] fields found in /Customer/Cart HTML - cannot checkout"; CHECKOUT_OK=0; dump_body "$TMP/cart.html"; }

  if [ "$CHECKOUT_OK" = 1 ]; then
    CO_TOK=$(tok "$TMP/cart.html")
    code=$(P "$JAR_CUST" "/Customer/Orders/Checkout" "$TMP/co.html" "${DATA_ARGS[@]}" \
            --data-urlencode "notes=harness order" --data-urlencode "__RequestVerificationToken=$CO_TOK" \
            -H "Content-Type: application/x-www-form-urlencoded")
    REDIR=$(loc "$TMP/co.html.h")
    if [ "$code" = 302 ] && ! echo "$REDIR" | grep -qi 'cart'; then
      ok "POST /Customer/Orders/Checkout -> 302 to $REDIR"
    else
      bad "checkout expected 302 away from cart, got $code -> $REDIR (TempData error likely shown on cart)"
      G "$JAR_CUST" "/Customer/Cart" "$TMP/cart_err.html" > /dev/null; dump_body "$TMP/cart_err.html"
    fi
    code=$(G "$JAR_CUST" "/Customer/Orders" "$TMP/corders.html")
    chk "GET /Customer/Orders" 200 "$code" "/Customer/Orders" "$TMP/corders.html"
    ORDER_ID=$(grep -oE '/Customer/Orders/Details/[0-9]+' "$TMP/corders.html" | grep -oE '[0-9]+$' | sort -n | tail -1)
    if need ORDER_ID "$ORDER_ID" "customer order id from /Customer/Orders"; then
      echo "       derived order id: $ORDER_ID"
      contains "customer order list contains the new order" "$TMP/corders.html" "/Customer/Orders/Details/$ORDER_ID"
      G "$JAR_CUST" "/Customer/Cart" "$TMP/cart2.html" > /dev/null
      not_contains "cart emptied after checkout" "$TMP/cart2.html" "$PRODUCT_NAME"
      G "$JAR_CUST" "/Customer/Orders/Details/$ORDER_ID" "$TMP/odetail.html" > /dev/null
      contains "order details initially shows Pending" "$TMP/odetail.html" "Pending"

      # ---- farmer drives the order ----
      code=$(G "$JAR_FARMER" "/Farmer/Orders" "$TMP/forders.html")
      chk "GET /Farmer/Orders (farmer)" 200 "$code" "/Farmer/Orders" "$TMP/forders.html"
      contains "/Farmer/Orders lists order $ORDER_ID" "$TMP/forders.html" "/Farmer/Orders/Details/$ORDER_ID"
      code=$(G "$JAR_FARMER" "/Farmer/Orders/Details/$ORDER_ID" "$TMP/fdetail.html")
      chk "GET /Farmer/Orders/Details/$ORDER_ID" 200 "$code" "/Farmer/Orders/Details/$ORDER_ID" "$TMP/fdetail.html"
      F_TOK=$(tok "$TMP/fdetail.html")

      drive() { # <action> <expected status text on customer page>
        local act="$1" want="$2"
        code=$(P "$JAR_FARMER" "/Farmer/Orders/$act/$ORDER_ID" "$TMP/drive.html" \
                --data-urlencode "__RequestVerificationToken=$F_TOK" -H "Content-Type: application/x-www-form-urlencoded")
        chk "POST /Farmer/Orders/$act/$ORDER_ID -> 302" 302 "$code" "/Farmer/Orders/$act/$ORDER_ID" "$TMP/drive.html"
        G "$JAR_CUST" "/Customer/Orders/Details/$ORDER_ID" "$TMP/odetail.html" > /dev/null
        contains "customer /Customer/Orders/Details/$ORDER_ID reflects '$want'" "$TMP/odetail.html" "$want"
      }
      drive Accept Accepted
      drive MarkReady ReadyForPickup
      drive Complete Completed

      code=$(G "$JAR_CUST" "/Customer/Orders/History" "$TMP/hist.html")
      chk "GET /Customer/Orders/History" 200 "$code" "/Customer/Orders/History" "$TMP/hist.html"
      contains "completed order $ORDER_ID appears in History" "$TMP/hist.html" "/Customer/Orders/Details/$ORDER_ID"
    fi
  fi
fi

# ============================================================================
section "6. NEW MARKET PAGE"
# ============================================================================
code=$(G "$JAR_CUST" "/Markets/Details/1" "$TMP/mka.html")
chk "GET /Markets/Details/1 (as customer)" 200 "$code" "/Markets/Details/1" "$TMP/mka.html"
contains "market page has 'Farms at this market'"  "$TMP/mka.html" "Farms at this market"
contains "market page has 'Fresh produce for pickup here'" "$TMP/mka.html" "Fresh produce for pickup here"
contains "market page has 'Pickup times'" "$TMP/mka.html" "Pickup times"
FARM_NAME=$(tr -d '\n\r' < "$TMP/mka.html" | grep -oP 'Farms at this market.{0,1200}?<h5[^>]*>\K[^<]+' | head -1)
if need FARM_NAME "$FARM_NAME" "farm name in stalls section"; then
  echo "       derived farm name: '$FARM_NAME'"
  contains "market page renders seeded farm name" "$TMP/mka.html" "$FARM_NAME"
fi
VISIT=$(tr -d '\n\r' < "$TMP/mka.html" | grep -oP 'href="\K/Farmers/Details/[0-9]+(?="[^>]*>[[:space:]]*(<i[^>]*></i>)?Visit farm)' | head -1)
[ -z "$VISIT" ] && VISIT=$(grep -oE '/Farmers/Details/[0-9]+' "$TMP/mka.html" | head -1)
if need VISIT "$VISIT" "'Visit farm' link on market page"; then
  code=$(G "" "$VISIT" "$TMP/visit.html"); chk "Visit farm link $VISIT resolves" 200 "$code" "$VISIT" "$TMP/visit.html"
fi

# ============================================================================
section "7. NOTIFICATIONS"
# ============================================================================
code=$(G "$JAR_CUST" "/Customer/Notifications" "$TMP/notifs.html")
chk "GET /Customer/Notifications (customer)" 200 "$code" "/Customer/Notifications" "$TMP/notifs.html"
G "$JAR_CUST" "/Customer/Dashboard" "$TMP/cdash2.html" > /dev/null
has_badge=0; grep -q 'notif-badge' "$TMP/cdash2.html" && has_badge=1
grep -q 'data-notifications-url=' "$TMP/cdash2.html" && has_badge=$((has_badge+1))
if [ "$has_badge" -ge 2 ]; then ok "customer layout carries unread badge + data-notifications-url"
elif [ "$has_badge" -ge 1 ]; then ok "customer layout carries notification badge/attribute marker"
else bad "no notif-badge / data-notifications-url on /Customer/Dashboard layout"; fi

# ============================================================================
echo
echo "PASS=$PASS FAIL=$FAIL"
[ "$FAIL" -eq 0 ] && exit 0 || exit 1
