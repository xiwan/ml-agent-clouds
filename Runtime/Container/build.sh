#!/bin/bash

# check params num
if [ "$#" -ne 3 ]; then
    echo "Usage: $0 <region_code> <account_id> <image_name>"
    exit 1
fi

region_code=$1
account_id=$2
image_name=$3

# verify region_code 
if ! echo "$region_code" | grep -q '^[a-z0-9\-]*$'; then
    echo "Invalid region_code: $region_code"
    exit 1
fi

# verify account_id 
if ! echo "$account_id" | grep -q '^[0-9]*$'; then
    echo "Invalid account_id: $account_id"
    exit 1
fi

# verify image_name 
if ! echo "$image_name" | grep -q '^[a-zA-Z0-9_\-]*$'; then
    echo "Invalid image_name: $image_name"
    exit 1
fi

echo "Step 1: Login to ECR"
aws ecr get-login-password --region ${region_code} | docker login --username AWS --password-stdin ${account_id}.dkr.ecr.us-east-1.amazonaws.com

echo "Step 2: Build Docker image"
docker build -t ${image_name} .

echo "Step 3: Tag Docker image, default is latest"
docker tag ${image_name}:latest ${account_id}.dkr.ecr.${region_code}.amazonaws.com/${image_name}:latest

echo "Step 4: Push Docker image to ECR"
docker push ${account_id}.dkr.ecr.${region_code}.amazonaws.com/${image_name}:latest