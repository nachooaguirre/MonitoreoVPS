import psycopg2

conn = psycopg2.connect("host=localhost dbname=superpos user=postgres password=Chupamela10")
cur = conn.cursor()

cur.execute('SELECT "Id", "Nombre", "EsCentral", "Activo" FROM "Sucursales";')
sucursales = cur.fetchall()
print("--- SUCURSALES ---")
for s in sucursales:
    print(f"ID: {s[0]}, Nombre: {s[1]}, EsCentral: {s[2]}, Activo: {s[3]}")

cur.close()
conn.close()
