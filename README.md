# Assignment 1

There are several types of products, each having a known, constant, unit price. In the begining, we know the quantity of each product.

We must keep track of the quantity of each product, the amount of money (initially zero), and the list of bills, corresponding to sales. Each bill is a list of items and quantities sold in a single operation, and their total price.

We have sale operations running concurrently, on several threads. Each sale decreases the amounts of available products (corresponding to the sold items), increases the amount of money, and adds a bill to a record of all sales.

From time to time, as well as at the end, an inventory check operation shall be run. It shall check that all the sold products and all the money are justified by the recorded bills.

Two sales involving distinct products must be able to update their quantities independently (without having to wait for the same mutex).
