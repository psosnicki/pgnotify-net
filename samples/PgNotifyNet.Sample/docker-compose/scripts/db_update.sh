#!/bin/bash
echo 'db updater starts'
apt-get -yq update
apt -yq install  postgresql-client
echo 'installed psql client'
connectionString="postgresql://$POSTGRES_USER:$POSTGRES_PASSWORD@db/$POSTGRES_DB"
i=1
while true
do
    
         categories=("Dried fruit and bean" "Meat" "Fresh fruits" "Seafood" "Fast food" "Fish")
         rnd=$(( ( RANDOM % ${#categories[@]} ) + 1 ))
         categoryId=$(( ( RANDOM % 6 ) + 1 ))
         psql "$connectionString" -c "UPDATE \"categories\" SET \"description\" = '${categories[$rnd - 1]}' where \"category_id\" = $categoryId";

    if [ $(( $i % 2 )) -eq 0 ]
    then
         psql "$connectionString" -c "INSERT  INTO \"categories\" (category_id,category_name,description) VALUES ($(( ( RANDOM % 1000) + 1 )),'foo category $i', 'Foo $i description' )";
    fi
    i=$((i+1))
    sleep 5
done

