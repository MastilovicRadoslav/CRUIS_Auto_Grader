def add(x, y): return x + y
def sub(x, y): return x - y
def mul(x, y): return x * y
def div(x, y): return x / y if y != 0 else "Dijeljenje sa nulom!"

operations = {
    "+": add,
    "-": sub,
    "*": mul,
    "/": div
}

print("🧮 Mini Kalkulator")
print("Dostupne operacije: +  -  *  /")

while True:
    expr = input("Unesi izraz (npr. 4 * 5) ili 'q' za izlaz: ")
    if expr.lower() == "q":
        print("Kraj programa.")
        break

    try:
        x, op, y = expr.split()
        x, y = float(x), float(y)
        if op in operations:
            result = operations[op](x, y)
            print(f"Rezultat: {result}\n")
        else:
            print("Nepoznata operacija!\n")
    except Exception:
        print("Nevažeći unos! Pokušaj ponovo.\n")
